using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.mediaList
{
    public class ImageFileModel: MediaFileBaseModel
    {
        public string photographer { get; set; }

        public override string fileType { get { return "Images"; } set { } }
    }
}
