using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.mediaList
{
    public class FinalAvFileModel : AuViFileModel
    {
        public override string fileType { get { return "Final"; } set { } }

        override public bool canPublish { get => true; set { } }
    }

}
