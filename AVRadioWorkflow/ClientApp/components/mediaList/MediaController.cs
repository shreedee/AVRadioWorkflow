using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CustomExtensions;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;

namespace components.mediaList
{
    [Route("api/[controller]")]
    public class MediaController : Controller
    {
        readonly ILogger _logger;

        readonly IStorageService _storage;
        
        public MediaController(
            IStorageService storage,
            ILogger<MediaController> logger
        )
        {
            _logger = logger;
            _storage = storage;
        }

        

        /// <summary>
        /// For this controller the client decides whihc path to put this file in 
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="createSignedURL"></param>
        /// <returns></returns>
        [HttpGet("newImageId")]
        public DirectUploadModel getNewImageId([FromQuery] string fileType, [FromQuery] string fileName, [FromQuery] string folderpath, [FromQuery] bool createSignedURL = false)
        {
            if (string.IsNullOrWhiteSpace(fileType))
                throw new ArgumentNullException("fileType");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            fileName = fileName.Replace(' ', '_');

            if (string.IsNullOrWhiteSpace(folderpath))
                throw new ArgumentNullException("folderpath");


            var mediaSplit = fileType.Split('/');
            if (mediaSplit.Length != 2)
                throw new bootCommon.ExceptionWithCode($"fileType : {fileType} is invalid");

            var mediaType = mediaSplit[0];

            MediaFileBaseModel mediafile = null;
            switch (mediaType)
            {
                case "image":
                    mediafile = new ImageFileModel();
                    mediafile.fileName = fileName;
                    break;
                case "audio":
                case "video":
                    mediafile = new AuViFileModel();
                    mediafile.fileName = $"{folderpath}_{fileName}";
                    break;
                default:
                    mediafile = new OtherFileModel();
                    mediafile.fileName = fileName;
                    break;
            }

            mediafile.path = $"{mediafile.fileType}/{mediafile.fileName}";


            var ret = new DirectUploadModel
            {
                mediaFile = mediafile,
                config = _storage.uploadConfig
            };

            var fullPath = $"{folderpath}/{mediafile.path}";

            _logger.LogDebug($"newPageidForUploadAsync: new page {fullPath}");

            if (createSignedURL)
            {
                if (!string.IsNullOrWhiteSpace(_storage.uploadConfig.aws_url))
                {
                    //we are using minio.. set up 
                    var origin = this.originFromURL("/api/media");

                    ret.keyForDirectUpload = _storage.createPresignedUrl(fullPath, true, origin);
                }
                else
                {
                    ret.keyForDirectUpload = _storage.createPresignedUrl(fullPath, true);
                }
            }
            else
            {
                ret.keyForDirectUpload = _storage.keyForDirectUpload(fullPath);
            }

            return ret;

        }

        /// <summary>
        /// creates a authorization signature to upload for AWS
        /// https://docs.aws.amazon.com/general/latest/gr/signature-v4-examples.html
        /// </summary>
        /// <param name="datetime"></param>
        /// <param name="to_sign"></param>
        /// <returns></returns>
        [HttpGet("uploadSignature")]
        public string uploadSignature([FromQuery]string datetime, [FromQuery]string to_sign, [FromQuery]string canonical_request)
        {
            return _storage.uploadSignature( datetime, to_sign, canonical_request);
        }

    }
}
