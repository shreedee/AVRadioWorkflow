using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.folderCreator
{
    public class PublishedLinkModel
    {
        public string wpLink { get; set; }
        public int wpPostId { get; set; }
        public DateTime lastModified { get; set; }
    }
}
