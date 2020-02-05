using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.folderCreator
{
    /// <summary>
    /// This should actually be called a publication details model. 
    /// name is for historical FOlderCreator -> publiMe model
    /// </summary>
    public class FolderDetailsModel
    {
        public DateTime recordingDate { get; set; }
        public string genre { get; set; }
        public string description { get; set; }
        public string language {get;set;}

        [bootCommon.ExportAsOptional]
        public string recordingBy { get; set; }

        [bootCommon.ExportAsOptional]
        public string savedFolder { get; set; }

        [bootCommon.ExportAsOptional]
        public PublishDetailsModel publishDetails { get; set; }

        [bootCommon.ExportAsOptional]
        public PublishedLinkModel publishedLink { get; set; }

    }
}
