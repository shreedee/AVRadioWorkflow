using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.mediaList
{
    public class MediaFileBaseModel
    {
        public string path { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual string fileType { get { return "Media"; } set { } } 

        public string title { get; set; }

        public string fileName { get; set; }
    }

    public class MediaFileBaseConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(MediaFileBaseModel);

        // this converter is only used for serialization, not to deserialize
        public override bool CanRead => true;

        // implement this if you need to read the string representation to create an AccountId
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var typeToken = token["fileType"];
            if (typeToken == null)
                throw new InvalidOperationException("invalid object");

            var actualType = typeof(ImageFileModel);

            if (existingValue == null || existingValue.GetType() != actualType)
            {
                var contract = serializer.ContractResolver.ResolveContract(actualType);
                existingValue = contract.DefaultCreator();
            }
            using (var subReader = token.CreateReader())
            {
                // Using "populate" avoids infinite recursion.
                serializer.Populate(subReader, existingValue);
            }
            return existingValue;
        }
            

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is MediaFileBaseModel))
                throw new JsonSerializationException("Expected MediaFileBaseModel object value.");

            var data = JsonConvert.SerializeObject(value, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            // custom response 
            writer.WriteValue(data);
        }
    }
}
