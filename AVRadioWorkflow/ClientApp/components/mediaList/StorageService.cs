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

using Newtonsoft.Json;

namespace components.mediaList
{
    public interface IStorageService
    {
        UploadConfigModel uploadConfig { get; }
        string createPresignedUrl(string publicPathORkey, bool forUpload = false, string overrideEndPoint = null);
        string keyForDirectUpload(string publicPathOrKey);
        string uploadSignature(string datetime, string to_sign, string canonical_request);

        Task SaveStream(string publicPathORkey, Stream stream);

        //Task<string> readAsync(string publicPathORkey);

        Task<string[]> getKeysByPrefix(string prefix);

        //Task copyObjectAsync(string from, string to);

        Task<ImageInfoModel> getImageInfoAsync(string publicPathORkey);

        Task<Stream> getStreamAsync(string publicPathORkey);
        Task<Stream> getImageStream(string publicPathORkey, int width);
        Task<AudioInfoModel> getAudioInfoAsync(string publicPathORkey);
        Task<bool> keyExists(string publicPathORkey);

        Task deleteFolderAsync(string publicPathORkey);
        Task copyFolderAsync(string from, string to);
    }

    public class s3Config: UploadConfigModel
    {
        [JsonIgnore]
        public string secretkey { get; set; }

    }


    public class StorageService : IStorageService
    {
        readonly ILogger _logger;

        readonly s3Config _uploadConfig;
        readonly folderCreator.MediaLocations _mediaLocations;


        public StorageService(
            IConfiguration config,
            ILogger<StorageService> logger
        )
        {
            _logger = logger;

            _uploadConfig = config.GetSection("s3Storage").Get<s3Config>();
            if(null == _uploadConfig)
            {
                throw new Exception("config section s3Storage not found");
            }

            _mediaLocations = config.GetSection("mediaLocations").Get<folderCreator.MediaLocations>();
            if (null == _mediaLocations)
                throw new Exception("config mediaLocations not found");


            
            

        }

        #region S3 Utilities
        Amazon.S3.AmazonS3Client createS3Client(string overrideEndPoint = null)
        {
            var awsRegion = Amazon.RegionEndpoint.GetBySystemName(_uploadConfig.region);
            if (string.IsNullOrWhiteSpace(_uploadConfig.customEndpoint))
            {
                return new Amazon.S3.AmazonS3Client(_uploadConfig.accesskey, _uploadConfig.secretkey, awsRegion);
            }
            else
            {
                var customRegion = new Amazon.S3.AmazonS3Config
                {
                    RegionEndpoint = awsRegion,
                    ServiceURL = _uploadConfig.customEndpoint,
                    ForcePathStyle = true,
                    UseHttp = _uploadConfig.endPointHttp
                };

                if (!string.IsNullOrWhiteSpace(overrideEndPoint))
                    customRegion.ServiceURL = overrideEndPoint;


                return new Amazon.S3.AmazonS3Client(_uploadConfig.accesskey, _uploadConfig.secretkey, customRegion);
            }

        }

        public struct StorageKey
        {
            public string bucket { get; set; }
            public string key { get; set; }
        }

        public static StorageKey getStorageKey(string appKey)
        {
            var split = appKey.Replace('\\', '/').Trim('/').Split('/');

            if (split.Length < 2)
            {
                throw new Exception($"invalid storage key {appKey}. Must have at least one levels of folders");
            }

            return new StorageKey
            {
                bucket = split[0],
                key = String.Join('/', split.Skip(1))
            };
            
        }

        string pathPrefix
        {
            get
            {
                return $"{_uploadConfig.customEndpoint}/";
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
            return  getKey(publicPathOrKey);
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
                _uploadConfig.region,
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
                key: _uploadConfig.secretkey,
                dateStamp: datetime.Substring(0, 8),
                regionName: _uploadConfig.region,
                serviceName: "s3"
                );

            var signature = HmacSHA256(to_sign, signing_key);

            // to lowercase hexits
            return ToHexString(signature);
        }

        #endregion


        public async Task deleteFolderAsync(string publicPathORkey)
        {
            var source = getStorageKey(getKey(publicPathORkey.Trim('/')));

            using (var s3Client = createS3Client())
            {
                while (true)
                {
                    var res = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = source.bucket,
                        Prefix = source.key,
                    });

                    if (0 == res.KeyCount)
                        break;

                    await s3Client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = source.bucket,
                        Objects = res.S3Objects.Select(o=> new KeyVersion {Key=o.Key}).ToList()
                    });
                }

            }
        }

        public async Task copyFolderAsync(string from, string to)
        {
            var source = getStorageKey(getKey(from.Trim('/')));
            var destination = getStorageKey(getKey(to.Trim('/')));

            using (var s3Client = createS3Client())
            {
                string ContinuationToken = null;
                while (true) {
                    var res = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = source.bucket,
                        Prefix = source.key,
                        ContinuationToken = ContinuationToken
                    });


                    var done = await Task.WhenAll(res.S3Objects.Select(async o =>
                    {

                        var fileDestination = o.Key.ReplaceInBegining(source.key, destination.key);

                        if (o.Key == fileDestination)
                            return true;

                        await s3Client.CopyObjectAsync(
                            source.bucket, o.Key,
                            destination.bucket, fileDestination
                        );

                        return true;
                    }));

                    if (string.IsNullOrWhiteSpace(res.ContinuationToken))
                    {
                        break;
                    }
                    else
                    {
                        ContinuationToken = res.ContinuationToken;
                    }
                }

            }
        }

        public async Task<bool> keyExists(string publicPathORkey)
        {
            var source = getStorageKey(getKey(publicPathORkey));
            using (var s3Client = createS3Client())
            {
                var res = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = source.bucket,
                    Prefix = source.key
                });

                return res.KeyCount > 0;
            }
        }

        public async Task<Stream> getImageStream(string publicPathORkey, int width)
        {
            var source = getStorageKey(getKey(publicPathORkey));
            using (var s3Client = createS3Client())
            {
                var res = await s3Client.GetObjectAsync(source.bucket, source.key);

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

        public Task<ImageInfoModel> getImageInfoAsync(string publicPathORkey)
        {

            var fullFIleName = Path.Combine(_mediaLocations.playgroundFolder, publicPathORkey);

            using (var image = Image.Load(fullFIleName))
            {
                return Task.FromResult(new ImageInfoModel
                {
                    width = image.Width,
                    height = image.Height
                });
            }

            
        }


        public async Task<AudioInfoModel> getAudioInfoAsync(string publicPathORkey)
        {
            var source = getStorageKey(getKey(publicPathORkey));
            using (var s3Client = createS3Client())
            {
                var res = await s3Client.GetObjectAsync(source.bucket, source.key);

                using (var audio = new WaveFileReader(res.ResponseStream))
                {
                    return new AudioInfoModel
                    {
                        duration = audio.TotalTime
                    };
                }

            }
        }


        public async Task copyObjectAsync(string from1,string to1)
        {
            try
            {
                var from = getStorageKey(getKey(from1));
                var to =getStorageKey(getKey(to1));

                using (var s3Client = createS3Client())
                {

                    await s3Client.CopyObjectAsync(
                        from.bucket, from.key,
                        to.bucket, to.key
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
                var key_prefix = getStorageKey(getKey(prefix));
                using (var s3Client = createS3Client())
                {
                    var res = (await s3Client.ListObjectsAsync(key_prefix.bucket, key_prefix.key));

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
                var key = getStorageKey(getKey(publicPathORkey));
                using (var s3Client = createS3Client())
                {
                    var res = await s3Client.GetObjectAsync(key.bucket, key.key);

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
            var key = getStorageKey(getKey(publicPathORkey));
            using (var s3Client = createS3Client())
            {
                await s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = key.bucket,
                    Key = key.key,
                    InputStream = stream
                });
            }
        }

        public string createPresignedUrl(string publicPathORkey, bool forUpload = false, string overrideEndPoint = null)
        {
            var key = getStorageKey(getKey(publicPathORkey));
            using (var s3Client = createS3Client(overrideEndPoint))
            {
                var request1 = new GetPreSignedUrlRequest
                {
                    BucketName = key.bucket,
                    Key = key.key,
                    Expires = DateTime.Now.AddMinutes(forUpload ? 60 : 5),
                    Verb = forUpload ? Amazon.S3.HttpVerb.PUT : Amazon.S3.HttpVerb.GET,
                };

                if (_uploadConfig.endPointHttp)
                    request1.Protocol = Amazon.S3.Protocol.HTTP;

                var ret = s3Client.GetPreSignedURL(request1);
                return ret;

            }

        }

        

       


        

    }
}
