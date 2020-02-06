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


namespace components.folderCreator
{
    [Route("api/[controller]")]
    public partial class FolderCreatorController : Controller
    {
        readonly ILogger _logger;
        readonly mediaList.IStorageService _storage;

        readonly string _templateRoot;
        readonly CreateOptionModel _createOptions = new CreateOptionModel();
        readonly mediaList.ImageInfoModel _desiredImageInfo = new mediaList.ImageInfoModel();

        readonly string _wp_url;

        readonly string[] _archiveLocations;
        readonly string _articlesRoot;

        public FolderCreatorController(
            IConfiguration configuration,
            mediaList.IStorageService storage,
            ILogger<FolderCreatorController> logger
        )
        {
            _logger = logger;
            _storage = storage;
            _templateRoot= configuration["mediaLocations:templates"];

            configuration.Bind("createOptions", _createOptions);
            configuration.Bind("desiredImageInfo", _desiredImageInfo);

            _wp_url = configuration["wordpress:url"];

            _archiveLocations = configuration.GetSection("mediaLocations:afterPublish").Get<string[]>();

            
            _articlesRoot = configuration["mediaLocations:articles"];
        }

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
                string jsonData = await _storage.readAsync(filename);
                ret.folderDetails = JsonConvert.DeserializeObject<FolderDetailsModel>(jsonData);

                ret.displayData = new DisplayDataModel
                {
                    externalLink = $"{_storage.uploadConfig.filesystemLink}/{ret.folderDetails.savedFolder}",
                    httpLinkPrefix = $"{_storage.uploadConfig.aws_url}/{_storage.uploadConfig.bucket}",
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
            var statusFileName = await SaveFolderAsync(data);
            var link = await PushToWP(data);

            var currentSavedFolder = data.savedFolder;

            string publishedLocation = null;

            foreach(var location in _archiveLocations)
            {
                data.savedFolder = currentSavedFolder.ReplaceInBegining(_articlesRoot, location);
                await _storage.copyFolderAsync(currentSavedFolder, data.savedFolder);

                var newlocation = await createNewFolder(data);

                if (null == publishedLocation)
                    publishedLocation = newlocation;
            }

            await _storage.deleteFolderAsync(currentSavedFolder);

            return publishedLocation;
        }




        Task<string[]> getTemplatesByGenreAsync(string genre) { return _storage.getKeysByPrefix($"{_templateRoot}/{genre}"); }

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

            var jsonData = JsonConvert.SerializeObject(data);
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
                    var rppData = await _storage.readAsync(t);
                    await Task.WhenAll(data.publishDetails.mediaFiles.Where(f => f is mediaList.AuViFileModel).Select(async f =>
                    {
                        var justfileName = Path.GetFileNameWithoutExtension(f.fileName);

                        var rppDataEdited = rppData.Replace(@"\\aleph\playground\Mix",$"Final");
                        await _storage.SaveStream(
                            $"{data.savedFolder}/{justfileName}.rpp",
                            new MemoryStream(Encoding.UTF8.GetBytes(rppDataEdited)));

                        //await _storage.copyObjectAsync(t, $"{data.savedFolder}/{justfileName}.rpp");

                    }));

                }
                else
                {
                    await _storage.copyObjectAsync(t, $"{data.savedFolder}/{templatFileName}");
                }


                
            }

            //create the FinalFolder With dummey status
            stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("{}"));
            await _storage.SaveStream($"{data.savedFolder}/Final/status.json", stream);

            return statusFileName;
        }
                

    }
}
