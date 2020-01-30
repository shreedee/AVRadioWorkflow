using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.mediaList
{
    public class UploadConfigModel
    {
        public string bucket { get; set; }
        public string aws_key { get; set; }
        public string awsRegion { get; set; }
        public string aws_url { get; set; }
        public string filesystemLink { get; set; }
    }
}
