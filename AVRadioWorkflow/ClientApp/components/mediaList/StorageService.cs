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
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using NAudio.Wave;

namespace components.mediaList
{
    public interface IStorageService
    {
        UploadConfigModel uploadConfig { get; }
        string createPresignedUrl(string publicPathORkey, bool forUpload = false, string overrideEndPoint = null);
        string keyForDirectUpload(string publicPathOrKey);
        string uploadSignature(string datetime, string to_sign, string canonical_request);

        Task SaveStream(string publicPathORkey, Stream stream);

        Task<string> readAsync(string publicPathORkey);

        Task<string[]> getKeysByPrefix(string prefix);

        Task copyObjectAsync(string from, string to);

        Task<ImageInfoModel> getImageInfoAsync(string publicPathORkey);

        Task<Stream> getStreamAsync(string publicPathORkey);
        Task<Stream> getImageStream(string publicPathORkey, int width);
        Task<AudioInfoModel> getAudioInfoAsync(string publicPathORkey);
    }


    public class StorageService : IStorageService
    {
        readonly ILogger _logger;

        readonly UploadConfigModel _uploadConfig;
        readonly string _awsSecretKey;
        readonly bool _s3UsesHttp = false;

        readonly string _s3customEndpoint;

        readonly string _storageRoot;


        public StorageService(
            IConfiguration configuration,
            ILogger<StorageService> logger
        )
        {
            _logger = logger;

            var section = configuration.GetSection("s3Storage");

            _s3customEndpoint = section["customEndpoint"];

            _uploadConfig = new UploadConfigModel
            {
                awsRegion = section["region"],
                aws_key = section["accesskey"],
                bucket = section["bucket"],
                aws_url = section["customEndpoint"],
                filesystemLink= section["filesystemLink"]
            };


            _awsSecretKey = section["secretkey"];

            var strs3UsesHttp = section["endPointHttp"];
            if (!string.IsNullOrWhiteSpace(strs3UsesHttp) && strs3UsesHttp.ToLower() == "true")
                _s3UsesHttp = true;

            _storageRoot = (section["StorageRoot"]??"").Trim('/');

            //Don't do it as with s3 api we can't configure it to be public
            ensureLocalBucket();

        }

        #region S3 Utilities
        Amazon.S3.AmazonS3Client createS3Client(string overrideEndPoint = null)
        {
            var awsRegion = Amazon.RegionEndpoint.GetBySystemName(_uploadConfig.awsRegion);
            if (string.IsNullOrWhiteSpace(_s3customEndpoint))
            {
                return new Amazon.S3.AmazonS3Client(_uploadConfig.aws_key, _awsSecretKey, awsRegion);
            }
            else
            {
                var customRegion = new Amazon.S3.AmazonS3Config
                {
                    RegionEndpoint = awsRegion,
                    ServiceURL = _s3customEndpoint,
                    ForcePathStyle = true,
                    UseHttp = _s3UsesHttp
                };

                if (!string.IsNullOrWhiteSpace(overrideEndPoint))
                    customRegion.ServiceURL = overrideEndPoint;


                return new Amazon.S3.AmazonS3Client(_uploadConfig.aws_key, _awsSecretKey, customRegion);
            }

        }

        static bool _localBucketChecked = false;
        void ensureLocalBucket()
        {
            if (_localBucketChecked)
                return;

            _localBucketChecked = true;

            if (string.IsNullOrWhiteSpace(_s3customEndpoint))
                return;

            if (string.IsNullOrWhiteSpace(_uploadConfig.bucket))
                throw new Exception("s3 bucket name not configured");

            if (string.IsNullOrWhiteSpace(_uploadConfig.awsRegion))
                throw new Exception("s3 bucketRegion name not configured");

            //todo: log
            using (var s3Client = createS3Client())
            {
                //we have to live with the depreciation because of Minio for now
                if (AmazonS3Util.DoesS3BucketExistAsync(s3Client, _uploadConfig.bucket).Result)
                    return;

                var putBucketResponse = s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _uploadConfig.bucket,
                    BucketRegionName = _uploadConfig.awsRegion,
                    CannedACL = S3CannedACL.PublicRead
                }).Result;

            }


        }

        string getStorageKey(string appKey)
        {
            if (!String.IsNullOrWhiteSpace(_storageRoot))
                appKey = $"{_storageRoot}/{appKey}";

            return appKey;
        }

        string pathPrefix
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_s3customEndpoint))
                {
                    return $"{_s3customEndpoint}/{_uploadConfig.bucket}/"
                      + (string.IsNullOrWhiteSpace(_storageRoot) ? "" : $"{_storageRoot}/");

                }

                return $"http://{_uploadConfig.bucket}.s3.amazonaws.com/"
                  + (string.IsNullOrWhiteSpace(_storageRoot) ? "" : $"{_storageRoot}/");
            }
        }

        public string publicPath(string key)
        {
            return $"{pathPrefix}{key}";
        }

        public string getKey(string publicPathORkey)
        {
            var key = publicPathORkey;
            if (key.StartsWith(pathPrefix))
                key = key.Replace(pathPrefix, "");

            return key;
        }

        public UploadConfigModel uploadConfig { get { return _uploadConfig; } }

        readonly static string[] _ALLOWEDDIRECTUPLODVERBS = new string[] { "POST", "PUT", "GET" };

        public string keyForDirectUpload(string publicPathOrKey)
        {
            return (string.IsNullOrWhiteSpace(_storageRoot) ? "" : $"{ _storageRoot}/") + getKey(publicPathOrKey);
        }

        static string ToHexString(byte[] data, bool lowercase = true)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString(lowercase ? "x2" : "X2"));
            }
            return sb.ToString();
        }

        //https://docs.aws.amazon.com/general/latest/gr/signature-v4-examples.html#signature-v4-examples-dotnet
        static byte[] HmacSHA256(String data, byte[] key)
        {
            var hashAlgorithm = new HMACSHA256(key);

            return hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        static byte[] getSignatureKey(String key, String dateStamp, String regionName, String serviceName)
        {
            byte[] kSecret = Encoding.UTF8.GetBytes(("AWS4" + key).ToCharArray());
            byte[] kDate = HmacSHA256(dateStamp, kSecret);
            byte[] kRegion = HmacSHA256(regionName, kDate);
            byte[] kService = HmacSHA256(serviceName, kRegion);
            byte[] kSigning = HmacSHA256("aws4_request", kService);

            return kSigning;
        }

        
        public string uploadSignature(string datetime, string to_sign, string canonical_request)
        {

            /* canonical_request
			 POST
/myelasticnetworkdata-dev/dee_dev_revcore2/rev_5be29cc43f45b1492cc4c4b9/page/beaba9d3-4d37-4347-bb0b-ed54382e2ae8/original.TIF
uploads=
host:s3-us-west-2.amazonaws.com
x-amz-date:20181107T080607Z

host;x-amz-date
e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
			*/

            var CRLines = canonical_request.Split('\n');
            if (CRLines.Length < 2)
            {
                _logger.LogWarning($"uploadSignature invalid canonical_request : {canonical_request}");
                throw new bootCommon.ExceptionWithCode($"canonical_request doesn't have enough lines", System.Net.HttpStatusCode.Forbidden);
            }

            if (!_ALLOWEDDIRECTUPLODVERBS.Contains(CRLines[0]))
            {
                _logger.LogWarning($"uploadSignature canonical_request verb not allowed. canonical_request: {canonical_request}");
                throw new bootCommon.ExceptionWithCode("invalid request", System.Net.HttpStatusCode.Forbidden);
            }

            /*
            var dummyageId = keyForDirectUpload("images/");
            var reqStartswith = $"/{_uploadConfig.bucket}/{dummyageId}";

            if (!CRLines[1].StartsWith(reqStartswith))
            {
                _logger.LogWarning($"uploadSignature canonical_request mismatch. reqStartswith :{reqStartswith} - canonical_request: {canonical_request}");
                throw new bootCommon.ExceptionWithCode("invalid request", System.Net.HttpStatusCode.Forbidden);
            }
            */

            var credsStrings = new[]
            {
                datetime.Substring(0, 8),
                _uploadConfig.awsRegion,
                "s3",
                "aws4_request"
            };

            using (var algorithm = SHA256.Create())
            {
                // Create the at_hash using the access token returned by CreateAccessTokenAsync.
                var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(canonical_request));
                var signParts = new[]
                {
                    "AWS4-HMAC-SHA256",
                    Uri.EscapeDataString(datetime),
                    string.Join('/', credsStrings),
                    ToHexString(hash)
                };


                var calculatedToSign = /*Uri.EscapeDataString*/
            (string.Join('\n', signParts));


                if (to_sign != calculatedToSign)
                {
                    throw new bootCommon.ExceptionWithCode("failed to sign");
                }
            }



            var signing_key = getSignatureKey(
                key: _awsSecretKey,
                dateStamp: datetime.Substring(0, 8),
                regionName: _uploadConfig.awsRegion,
                serviceName: "s3"
                );

            var signature = HmacSHA256(to_sign, signing_key);

            // to lowercase hexits
            return ToHexString(signature);
        }

        #endregion


        public async Task<Stream> getImageStream(string publicPathORkey, int width)
        {
            var key = getKey(publicPathORkey);
            using (var s3Client = createS3Client())
            {
                var res = await s3Client.GetObjectAsync(_uploadConfig.bucket, getStorageKey(key));

                var ms = new MemoryStream();
                {
                    await res.ResponseStream.CopyToAsync(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    using (var image = Image.Load(ms, out IImageFormat format))
                    {
                        if(image.Width == width)
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            return ms;
                        }

                        var height = image.Height * (width / image.Width);

                        image.Mutate(
                            i => i.Resize(width, height)
                                  );

                        var outStream = new MemoryStream();
                        image.Save(outStream, format);

                        outStream.Seek(0, SeekOrigin.Begin);
                        return outStream;
                    }
                }
            }

        }

        public async Task<ImageInfoModel> getImageInfoAsync(string publicPathORkey)
        {
            var key = getKey(publicPathORkey);
            using (var s3Client = createS3Client())
            {
                var res = await s3Client.GetObjectAsync(_uploadConfig.bucket, getStorageKey(key));

                using (var image = Image.Load(res.ResponseStream))
                {
                    return new ImageInfoModel
                    {
                        width = image.Width,
                        height = image.Height
                    };
                }

            }
        }


        public async Task<AudioInfoModel> getAudioInfoAsync(string publicPathORkey)
        {
            var key = getKey(publicPathORkey);
            using (var s3Client = createS3Client())
            {
                var res = await s3Client.GetObjectAsync(_uploadConfig.bucket, getStorageKey(key));

                using (var audio = new WaveFileReader(res.ResponseStream))
                {
                    return new AudioInfoModel
                    {
                        duration = audio.TotalTime
                    };
                }

            }
        }


        public async Task copyObjectAsync(string from,string to)
        {
            try
            {
                using (var s3Client = createS3Client())
                {

                    await s3Client.CopyObjectAsync(
                        _uploadConfig.bucket, getStorageKey(getKey(from)),
                        _uploadConfig.bucket, getStorageKey(getKey(to))
                        );
                }
            }
            catch (AmazonS3Exception ex)
            {
                throw new bootCommon.ExceptionWithCode("Failed to search", innerException: ex);
            }
        }

        public async Task<string[]> getKeysByPrefix(string prefix)
        {
            try
            {
                var key_prefix = getKey(prefix);
                using (var s3Client = createS3Client())
                {
                    var res = (await s3Client.ListObjectsAsync(_uploadConfig.bucket, getStorageKey(key_prefix)));

                    return res.S3Objects.Select(o => publicPath(o.Key)).ToArray();
                }
            }
            catch (AmazonS3Exception ex)
            {
                throw new bootCommon.ExceptionWithCode("Failed to search", innerException: ex);
            }
        }

       

        public async Task<Stream> getStreamAsync(string publicPathORkey)
        {
            try
            {
                var key = getKey(publicPathORkey);
                using (var s3Client = createS3Client())
                {
                    var res = await s3Client.GetObjectAsync(_uploadConfig.bucket, getStorageKey(key));

                    var ms = new MemoryStream();
                    await res.ResponseStream.CopyToAsync(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms;
                }
            }
            catch (Exception ex)
            {
                throw new bootCommon.ExceptionWithCode("file not found", innerException: ex);
            }

        }

        public async Task<string> readAsync(string publicPathORkey)
        {
            try
            {
                var reader = new StreamReader(await getStreamAsync(publicPathORkey));
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new bootCommon.ExceptionWithCode("file not found", innerException: ex);
            }

        }

        public async Task SaveStream(string publicPathORkey, Stream stream)
        {
            var key = getKey(publicPathORkey);
            using (var s3Client = createS3Client())
            {
                await s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _uploadConfig.bucket,
                    Key = getStorageKey(key),
                    InputStream = stream
                });
            }
        }

        public string createPresignedUrl(string publicPathORkey, bool forUpload = false, string overrideEndPoint = null)
        {
            var key = getKey(publicPathORkey);
            using (var s3Client = createS3Client(overrideEndPoint))
            {
                var request1 = new GetPreSignedUrlRequest
                {
                    BucketName = _uploadConfig.bucket,
                    Key = getStorageKey(key),
                    Expires = DateTime.Now.AddMinutes(forUpload ? 60 : 5),
                    Verb = forUpload ? Amazon.S3.HttpVerb.PUT : Amazon.S3.HttpVerb.GET,
                };

                if (_s3UsesHttp)
                    request1.Protocol = Amazon.S3.Protocol.HTTP;

                var ret = s3Client.GetPreSignedURL(request1);
                return ret;

            }

        }

        

       


        

    }
}
