using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.folderCreator
{
    /// <summary>
    /// Used to note publish Actions
    /// </summary>
    public class PublishedLinkModel
    {
        public string message { get; set; }

        public string wpLink { get; set; }
        public int wpPostId { get; set; }
        
        
        public DateTime lastModified { get; set; }
    }
}
