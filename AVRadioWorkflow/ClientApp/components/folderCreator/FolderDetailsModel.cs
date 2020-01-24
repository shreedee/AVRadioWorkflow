using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.folderCreator
{
    public class FolderDetailsModel
    {
        public DateTime recordingDate { get; set; }
        public string genre { get; set; }
        public string description { get; set; }
        public string language {get;set;}

        [bootCommon.ExportAsOptional]
        public string recordingBy { get; set; }

        [bootCommon.ExportAsOptional]
        public string[] mediaFiles { get; set; }

        [bootCommon.ExportAsOptional]
        public string savedFolder { get; set; }
    }
}
