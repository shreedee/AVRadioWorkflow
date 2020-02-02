using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.mediaList
{
    public class DirectUploadModel
    {
        public MediaFileBaseModel mediaFile { get; set; }

        /// <summary>
        /// the presigned upload URL
        /// </summary>
        public string keyForDirectUpload { get; set; }

        public UploadConfigModel config { get; set; }

        public string rootFolder { get; set; }
    }
}
