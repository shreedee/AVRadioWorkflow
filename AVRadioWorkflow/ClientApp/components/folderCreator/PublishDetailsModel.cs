using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.folderCreator
{
    public class PublishDetailsModel
    {

        public string title { get; set; }

        /// <summary>
        /// also used as subtitle
        /// </summary>
        [bootCommon.ExportAsOptional]
        public string twiterTitle { get; set; }

        public string bodyText { get; set; }

        [bootCommon.ExportAsOptional]
        public string programBy { get; set; }

        [bootCommon.ExportAsOptional]
        public string category { get; set; }

        [JsonConverter(typeof(mediaList.MediaFileBaseConverter))]
        public mediaList.MediaFileBaseModel[] mediaFiles { get; set; }
    }
}
