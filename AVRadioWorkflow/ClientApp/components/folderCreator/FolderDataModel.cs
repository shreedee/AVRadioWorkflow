using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.folderCreator
{
    public class FolderDataModel
    {
        public CreateOptionModel createOptions { get; set; }
        public FolderDetailsModel folderDetails { get; set; }

        public DisplayDataModel displayData { get; set; }
    }
}
