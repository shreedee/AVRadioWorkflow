using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.folderCreator
{
    public class DisplayDataModel
    {
        //The prefix where the files are stored
        public string externalLink { get; set; }

        //the prefix to view images from minio
        public string httpLinkPrefix { get; set; }

        public mediaList.ImageInfoModel desiredImageInfo { get; set; }

    }
}
