using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.mediaList
{
    public class DirectUploadModel
    {
        /// <summary>
        /// id of the new image
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// the presigned upload URL
        /// </summary>
        public string keyForDirectUpload { get; set; }

        public UploadConfigModel config { get; set; }
    }
}
