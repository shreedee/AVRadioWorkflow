using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.mediaList
{
    [JsonConverter(typeof(MediaFileBaseConverter))]
    public class MediaFileBaseModel
    {
        /// <summary>
        /// where the  original uploaded file is
        /// </summary>
        public string path { get; set; }

        /// <summary>
        /// Subtypes can cnahge if the final file should be found somewhere else
        /// </summary>
        public virtual string proccessedPath { get { return path; } set { } }


        public string objectType { get { return this.GetType().Name; } set { } }

        /// <summary>
        /// 
        /// </summary>
        public virtual string fileType { get { return "Media"; } set { } }

        public string title { get; set; }

        public string fileName { get; set; }

        /// <summary>
        /// if it's possible to publish
        /// </summary>
        virtual public bool canPublish { get; set; }

        /// <summary>
        /// User can set this flag to supress something from getting published
        /// </summary>
        virtual public bool doNotPublish { get; set; }


        public readonly static string[] SupportedImagesTypes = new[] { "png", "jpg", "jpeg", "gif" };

        public readonly static string[] SupportedAVTypes = new[] { "mp3", "mp4", "avi", "wav", "mpeg" };


        public static MediaFileBaseModel NewMediaFileFromMediaType(string mediaType, string fileName)
        {
            MediaFileBaseModel mediafile = null;

            switch (mediaType)
            {
                case "image":
                case "Images":
                    mediafile = new ImageFileModel();
                    break;
                case "Original":
                case "audio":
                case "video":
                    mediafile = new AuViFileModel();
                    break;
                case "Final":
                    mediafile = new FinalAvFileModel();
                    break;
                default:
                    mediafile = new OtherFileModel();
                    break;
            }

            mediafile.fileName = fileName;
            mediafile.path = $"{mediafile.fileType}/{mediafile.fileName}";

            return mediafile;
        }

        public static MediaFileBaseModel mediaObjectFromMimeType(string mimeType, string fileName)
        {
            var mediaSplit = mimeType.Split('/');
            if (mediaSplit.Length != 2)
                throw new bootCommon.ExceptionWithCode($"fileType : {mimeType} is invalid");

            var mediaType = mediaSplit[0];
            if ("image" == mediaType)
            {
                if (!MediaFileBaseModel.SupportedImagesTypes.Contains(mediaSplit[1].ToLower()))
                {
                    mediaType = "other";
                }
            }

            return NewMediaFileFromMediaType(mediaType, fileName);

        }
    }

    public class MediaFileBaseConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType.IsSubclassOf( typeof(MediaFileBaseModel));

        // this converter is only used for serialization, not to deserialize
        public override bool CanRead => true;

        // implement this if you need to read the string representation to create an AccountId
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var typeToken = token["objectType"];
            if (typeToken == null)
                throw new InvalidOperationException("invalid object");

            var strTYpe = $"components.mediaList.{typeToken}";

            var actualType = Type.GetType(strTYpe);

            if(!actualType.IsSubclassOf(typeof(MediaFileBaseModel)))
                throw new InvalidOperationException("invalid object type");

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

            if (!this.CanConvert(value.GetType()))
            {
                return;
            }

            var entity = value as MediaFileBaseModel;
            if (entity == null) return;
            
            writer.WriteStartObject();
            var props = entity.GetType().GetProperties();
            foreach (var propertyInfo in props)
            {
                var tempVal = propertyInfo.GetValue(entity);
                if (tempVal == null) continue;

                writer.WritePropertyName(propertyInfo.Name);
                serializer.Serialize(writer, tempVal);
            }

            writer.WriteEndObject();
        }
    }
}
