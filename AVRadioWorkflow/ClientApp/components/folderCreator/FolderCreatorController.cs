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

namespace components.folderCreator
{
    [Route("api/[controller]")]
    public class FolderCreatorController : Controller
    {
        readonly ILogger _logger;
        readonly mediaList.IStorageService _storage;

        public FolderCreatorController(
            mediaList.IStorageService storage,
            ILogger<FolderCreatorController> logger
        )
        {
            _logger = logger;
            _storage = storage;
        }

        [HttpGet("options")]
        public CreateOptionModel getOptions()
        {
            return new CreateOptionModel
            {
                //default Production
                availableGenres = new[] { "Interview", "Meeting", "Music", "News", "Performance", "Presentation", "Production", "Reading", "Theater" }.OrderBy(l => l).ToArray(),
                availableLanguage = new[] { "English", "Italian", "Tamil", "French", "Hindi", "Spanish", "German", "Russian", "Bengali" }.OrderBy(l => l).ToArray(),
                
                defaultGenre = "Production",
                defaultLanguage = "English"
            };

            
        }

        [HttpGet("load")]
        public async Task<FolderDetailsModel> LoadFolderDetails([FromQuery]string filename)
        {
            
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentNullException("filename");

            string jsonData = await _storage.readAsync(filename);
            return JsonConvert.DeserializeObject<FolderDetailsModel>(jsonData);
        }

        [HttpPost]
        public async Task SaveFolderDetails([FromBody]FolderDetailsModel data)
        {
            if (string.IsNullOrWhiteSpace(data.savedFolder))
                throw new Exception("savedfolder is empty");

            var jsonData = JsonConvert.SerializeObject(data);
            var fileName = $"{data.savedFolder}/sessionData.json";

            var stream = new System.IO.MemoryStream(Encoding.ASCII.GetBytes(jsonData));

            await _storage.SaveStream(fileName, stream);

            var origin = this.originFromURL("/api/foldercreator");

            var shortcut = $"[InternetShortcut]\r\nURL={origin}/foldercreator?filename={System.Uri.EscapeDataString(fileName) }";

            stream = new System.IO.MemoryStream(Encoding.ASCII.GetBytes(shortcut));

            await _storage.SaveStream($"{data.savedFolder}/folderCreator.url", stream);

        }
                

    }
}
