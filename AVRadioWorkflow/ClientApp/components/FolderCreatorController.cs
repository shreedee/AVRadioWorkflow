using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClientApp.components
{
    [Route("api/[controller]")]
    public class FolderCreatorController : Controller
    {
        readonly ILogger _logger;

        public FolderCreatorController(
            
            ILogger<FolderCreatorController> logger
        )
        {
            _logger = logger;
        }

        [HttpGet("availablelangs")]
        public string[] getAvailableLanguages()
        {
            return new[] { "english", "italian", "tamil", "french", "hindi", "spanish" }.OrderBy(l => l).ToArray();
        }

        [HttpGet("availablegenres")]
        public string[] getAvailableGenres()
        {
            return new[] { "hiphop", "bass"}.OrderBy(l => l).ToArray();
        }

    }
}
