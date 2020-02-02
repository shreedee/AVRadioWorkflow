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
    public class FolderCreatorController : Controller
    {
        readonly ILogger _logger;
        readonly mediaList.IStorageService _storage;

        readonly string _templateRoot;
        readonly CreateOptionModel _createOptions = new CreateOptionModel();


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
                ret.externalLink = $"{_storage.uploadConfig.filesystemLink}/{ret.folderDetails.savedFolder}";
            }

            return ret;

        }


        Task<string[]> getTemplatesByGenreAsync(string genre) { return _storage.getKeysByPrefix($"{_templateRoot}/{genre}"); }

        [HttpPost("save")]
        public async Task<string> SaveFolderAsync([FromBody]FolderDetailsModel data)
        {
            if (string.IsNullOrWhiteSpace(data.savedFolder))
                throw new bootCommon.ExceptionWithCode("savedfolder is empty");

            if (null == data.publishDetails || null == data.publishDetails.mediaFiles || 0 == data.publishDetails.mediaFiles.Length)
            {
                throw new bootCommon.ExceptionWithCode("no media files");
            }

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
                await _storage.copyObjectAsync(t, $"{data.savedFolder}/{Path.GetFileName(t)}");
            }

            //create the FinalFolder With dummey status
            stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("{}"));
            await _storage.SaveStream($"{data.savedFolder}/Final/status.json", stream);

            return statusFileName;
        }
                

    }
}
