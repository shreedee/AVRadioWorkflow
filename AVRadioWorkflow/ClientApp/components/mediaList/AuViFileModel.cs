using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.mediaList
{
    public class AuViFileModel : MediaFileBaseModel
    {
        public override string fileType { get { return "Original"; } set { } }

        public AudioInfoModel info { get; set; }

        override public bool canPublish { get=>false; set { } }

    }

        


}
