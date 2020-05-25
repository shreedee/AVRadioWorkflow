using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.folderCreator
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublishStatusModel
    {
        notPublished,
        publishRequested,
        publishCompleted

    }
}
