using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CustomExtensions;
using System.IO;
using MassTransit;
using System.Threading;

namespace components.folderCreator
{
    /// <summary>
    /// locations config . settings are here even if used in other micro services
    /// </summary>
    public class MediaLocations
    {
        /// <summary>
        /// subfolder where Pre-Published articles are at 
        /// can be reached from minio with this key or from filesystem with this key as well
        /// </summary>
        public string articlesRoot { get; set; }

        /// <summary>
        /// subforlder moved when things are waiting to be published
        /// </summary>
        public string toPublish { get; set; }

        /// <summary>
        /// subfolder moved to this location when things have been published
        /// </summary>
        public string publishDone { get; set; }


        /// <summary>
        /// fully resolved fileSystem link For MINIO ROOT
        /// </summary>
        public string playgroundFolder{ get; set; }


        /// <summary>
        /// fully resolved fileSystem link where to CopyTemplates from 
        /// </summary>
        public string templatesFolder { get; set; }


    }

    [Route("api/[controller]")]
    public partial class FolderCreatorController : Controller
    {
        readonly ILogger _logger;
        readonly mediaList.IStorageService _storage;

        
        readonly CreateOptionModel _createOptions = new CreateOptionModel();
        readonly mediaList.ImageInfoModel _desiredImageInfo = new mediaList.ImageInfoModel();

        //readonly string _wp_url;
        readonly IBus _mqBus;

        readonly MediaLocations _mediaLocations;

        public FolderCreatorController(
            IConfiguration configuration,
            mediaList.IStorageService storage,
            IBus mqBus,
            ILogger<FolderCreatorController> logger
        )
        {
            _mqBus = mqBus;
            _logger = logger;
            _storage = storage;
            //_templateRoot= configuration["mediaLocations:templates"];

            configuration.Bind("createOptions", _createOptions);
            configuration.Bind("desiredImageInfo", _desiredImageInfo);

            //_wp_url = configuration["wordpress:url"];

            //_archiveLocations = configuration.GetSection("mediaLocations:afterPublish").Get<string[]>();

            _mediaLocations = configuration.GetSection("mediaLocations").Get<MediaLocations>();
            if (null == _mediaLocations)
                throw new Exception("config mediaLocations not found");


        }

        /// <summary>
        /// Loading Folder details from a given File
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        [HttpGet("load")]
        public async Task<FolderDataModel> LoadFolderDetails([FromQuery]string filename)
        {
            var ret = new FolderDataModel
            {
                createOptions = _createOptions
            };

            if (string.IsNullOrWhiteSpace(filename))
            {
                ret.folderDetails = new FolderDetailsModel
                {
                    recordingDate = DateTime.Now,
                    genre = ret.createOptions.defaultGenre,
                    language = ret.createOptions.defaultLanguage
                };
            }
            else
            {
                var fullFileName = Path.Combine(_mediaLocations.playgroundFolder, filename);

                string jsonData = await System.IO.File.ReadAllTextAsync(fullFileName);
                
                ret.folderDetails = JsonConvert.DeserializeObject<FolderDetailsModel>(jsonData);

                var savedFolder = Path.GetDirectoryName(fullFileName);

                var foundMedia = (new[] { "Media", "Images", "Original", "Final" }).SelectMany(mediaType =>
                 {
                     var subPath = Path.Combine(savedFolder, mediaType);
                     if (!Directory.Exists(subPath)) {
                         return new mediaList.MediaFileBaseModel[] { };
                     }

                     return Directory.GetFiles(subPath)
                     .Select(f => Path.GetFileName(f))
                     .Where(f => f != "status.json")
                     .Select(f => mediaList.MediaFileBaseModel.NewMediaFileFromMediaType(mediaType, f));
                     
                 }).ToArray();

                var existingPaths = ret.folderDetails.publishDetails.mediaFiles.Select(f => f.path);

                foundMedia = foundMedia.Where(f => !existingPaths.Contains(f.path)).ToArray();

                if (foundMedia.Length > 0) {
                    ret.folderDetails.publishDetails.mediaFiles = ret.folderDetails.publishDetails.mediaFiles.Concat(foundMedia).ToArray();
                    await SaveFolderAsync(ret.folderDetails);
                }

                ret.displayData = new DisplayDataModel
                {
                    //externalLink = $"{_storage.uploadConfig.filesystemLink}/{ret.folderDetails.savedFolder}",
                    httpLinkPrefix = $"{_storage.uploadConfig.customEndpoint}",
                    desiredImageInfo = _desiredImageInfo
                };
            }

            return ret;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>The new published location </returns>
        [HttpPost("publish")]
        public async Task<String> Publish([FromBody]FolderDetailsModel data)
        {
            if (data.publishStatus != PublishStatusModel.publishRequested)
            {
                data.publishStatus = PublishStatusModel.publishRequested;

                data.publishedActions = (data.publishedActions ?? new PublishedLinkModel[] { }).Concat(new[] {new PublishedLinkModel{
                lastModified= DateTime.Now,
                message= "publish requested"
                } }).ToArray();
            }

            var statusFileName = await SaveFolderAsync(data);

            var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await _mqBus.Send(new neSchedular.ExecuteJobTask
            {
                jobName = "publishPost",
                JobParam = data.savedFolder
            },ct.Token);

            return statusFileName;
            
        }




        Task<string[]> getTemplatesByGenreAsync(string genre) 
        {
            if (!Directory.Exists(_mediaLocations.templatesFolder))
                throw new bootCommon.ExceptionWithCode($"Template folder {_mediaLocations.templatesFolder} does not exist");

            var ret = Directory.GetFiles(_mediaLocations.templatesFolder, $"{genre}*");

            return Task.FromResult( ret);
            
            //return _storage.getKeysByPrefix($"{_templateRoot}/{genre}"); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Statusfile name</returns>
        [HttpPost("save")]
        public async Task<string> SaveFolderAsync([FromBody]FolderDetailsModel data)
        {
            if (string.IsNullOrWhiteSpace(data.savedFolder))
                throw new bootCommon.ExceptionWithCode("savedfolder is empty");

            if (null == data.publishDetails || null == data.publishDetails.mediaFiles || 0 == data.publishDetails.mediaFiles.Length)
            {
                throw new bootCommon.ExceptionWithCode("no media files");
            }

            {
                var unInitializedMediaFile = data.publishDetails.mediaFiles.Where(f =>
                                f.objectType == typeof(mediaList.ImageFileModel).Name
                                && null == ((mediaList.ImageFileModel)f).imageInfo)
                        .Cast<mediaList.ImageFileModel>().ToArray();

                await Task.WhenAll(unInitializedMediaFile.Select(async f =>
                {
                    f.imageInfo = await _storage.getImageInfoAsync($"{data.savedFolder}/{f.path}");

                    var tolerance = ((f.imageInfo.height * 1.0) / f.imageInfo.width) / ((_desiredImageInfo.height * 1.0) / _desiredImageInfo.width);

                    f.canPublish = tolerance >= 0.87 && tolerance < 1.05;

                    return true;
                }));
            }

            {/*
                var unInitializedMediaFile = data.publishDetails.mediaFiles.Where(f =>
                                f.objectType == typeof(mediaList.AuViFileModel).Name
                                && null == ((mediaList.AuViFileModel)f).info)
                        .Cast<mediaList.AuViFileModel>().ToArray();

                await Task.WhenAll(unInitializedMediaFile.Select(async f =>
                {
                    f.info = await _storage.getAudioInfoAsync($"{data.savedFolder}/{f.path}");
                    return true;
                }));
            */}

            if (string.IsNullOrWhiteSpace(data.description))
            {
                throw new bootCommon.ExceptionWithCode("no description");
            }

            if (string.IsNullOrWhiteSpace(data.publishDetails.title))
                data.publishDetails.title = data.description;

            var jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
            var statusFileName = $"{data.savedFolder}/sessionData.json";

            var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonData));

            await _storage.SaveStream(statusFileName, stream);

            return statusFileName;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>key for the saved session status file</returns>
        [HttpPost("newFolder")]
        public async Task<string> createNewFolder([FromBody]FolderDetailsModel data)
        {
            var statusFileName = await SaveFolderAsync(data);

            var origin = this.originFromURL("/api/foldercreator");

            var shortcut = $"[InternetShortcut]\r\nURL={origin}/publiMe?filename={System.Uri.EscapeDataString(statusFileName) }";

            var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(shortcut));

            await _storage.SaveStream($"{data.savedFolder}/avRadioPublisher.url", stream);

            var templatePrefix = "Other_";
            var otherTemplates = await getTemplatesByGenreAsync(templatePrefix);

            var templatemap = otherTemplates.ToDictionary(k =>  k.Replace(templatePrefix, ""), v => v);

            templatePrefix = $"{data.genre}_";
            var genre_templates = await getTemplatesByGenreAsync(templatePrefix);
            foreach(var t in genre_templates)
            {
                templatemap[t.Replace(templatePrefix, "")] = t;
            }

            foreach(var t in templatemap.Values)
            {
                var templatFileName = Path.GetFileName(t);

                var ext = Path.GetExtension(templatFileName).Trim('.');

                if("rpp" == ext)
                {
                    var rppData = await System.IO.File.ReadAllTextAsync(t);
                    await Task.WhenAll(data.publishDetails.mediaFiles.Where(f => f is mediaList.AuViFileModel).Select(async f =>
                    {
                        var justfileName = Path.GetFileNameWithoutExtension(f.fileName);

                        var rppDataEdited = rppData.Replace(@"\\aleph\playground\Mix",$"Final");
                        await _storage.SaveStream(
                            $"{data.savedFolder}/{justfileName}.rpp",
                            new MemoryStream(Encoding.UTF8.GetBytes(rppDataEdited)));

                        var dest = Path.Combine(_mediaLocations.playgroundFolder, data.savedFolder, templatFileName);
                        System.IO.File.Copy(t, dest);

                        //                        await _storage.copyObjectAsync(t, $"{data.savedFolder}/{justfileName}.rpp");

                    }));

                }
                else
                {
                    //await _storage.copyObjectAsync(t, $"{data.savedFolder}/{templatFileName}");

                    var dest = Path.Combine(_mediaLocations.playgroundFolder, data.savedFolder, templatFileName);

                    System.IO.File.Copy(t, dest);
                }


                
            }

            //create the FinalFolder With dummey status
            stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("{}"));
            await _storage.SaveStream($"{data.savedFolder}/Final/status.json", stream);

            return statusFileName;
        }
                

    }
}
