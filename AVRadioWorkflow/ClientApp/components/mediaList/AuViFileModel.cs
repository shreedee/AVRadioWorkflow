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

        public override string proccessedPath {
            get {
                if (string.IsNullOrWhiteSpace(path))
                    return path;

                var justfile = System.IO.Path.GetFileName(path);
                return $"Final/{justfile}";
            }
            set { }
        }
    }
}
