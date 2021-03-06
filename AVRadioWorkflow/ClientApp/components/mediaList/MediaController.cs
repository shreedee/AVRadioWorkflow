﻿using System;
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
using System.IO;
using System.Text.RegularExpressions;

namespace components.mediaList
{
    [Route("api/[controller]")]
    public class MediaController : Controller
    {
        readonly ILogger _logger;
        readonly IStorageService _storage;
        readonly folderCreator.MediaLocations _mediaLocations;

        public MediaController(
            IStorageService storage,
            IConfiguration configuration,
            ILogger<MediaController> logger
        )
        {
            _logger = logger;
            _storage = storage;

            _mediaLocations = configuration.GetSection("mediaLocations").Get<folderCreator.MediaLocations>();
            if (null == _mediaLocations)
                throw new Exception("config mediaLocations not found");

        }


        readonly static Regex _filenameRegex = new Regex(@"[^a-zA-Z0-9\.]");
        readonly static Regex _pathRegex = new Regex(@"[^a-zA-Z0-9/\.]");

        

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

            if (string.IsNullOrWhiteSpace(folderpath))
                throw new ArgumentNullException("folderpath");

            
            fileName = _filenameRegex.Replace(fileName, "_");

            var orgFolderPath = folderpath = _pathRegex.Replace(folderpath, "_");

            if (!folderpath.StartsWith(_mediaLocations.articlesRoot))
                folderpath = $"{_mediaLocations.articlesRoot}/{folderpath}";

            /*
            fileName = fileName.Replace(' ', '_');
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

            folderpath = folderpath.Replace(' ', '_');
            folderpath = string.Join("_", folderpath.Split(Path.GetInvalidPathChars()));
            */
            
            var mediafile2 = MediaFileBaseModel.mediaObjectFromMimeType(fileType, fileName) ;
            

            var ret = new DirectUploadModel
            {
                mediaFile = mediafile2,
                config = _storage.uploadConfig,
                rootFolder = folderpath
            };


            var storageKey = StorageService.getStorageKey($"{folderpath}/{mediafile2.path}");

            _logger.LogDebug($"newPageidForUploadAsync: new page {storageKey.bucket}:{storageKey.key}");

            ret.config.bucket = storageKey.bucket;


            if (createSignedURL)
            {
                if (!string.IsNullOrWhiteSpace(_storage.uploadConfig.customEndpoint))
                {
                    //we are using minio.. set up 
                    var origin = this.originFromURL("/api/media");

                    ret.keyForDirectUpload = _storage.createPresignedUrl(storageKey.key, true, origin);
                }
                else
                {
                    ret.keyForDirectUpload = _storage.createPresignedUrl(storageKey.key, true);
                }
            }
            else
            {
                ret.keyForDirectUpload = _storage.keyForDirectUpload(storageKey.key);
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
