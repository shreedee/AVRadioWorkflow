using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.folderCreator
{
    public class CreateOptionModel
    {
        public string[] availableLanguage { get; set; }
        public string[] availableGenres { get; set; }

        public string defaultLanguage { get; set; }
        public string defaultGenre { get; set; } 
    }
}
