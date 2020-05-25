using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace components.mediaList
{
    /// <summary>
    /// DONOT change member of this as it is passed to evaporate JS as well
    /// </summary>
    public class UploadConfigModel
    {
        
        public string accesskey { get; set; }
        public string region { get; set; }
        public string customEndpoint { get; set; }

        public bool endPointHttp { get; set; }

        /*** props needed by evaporateJS****/
        public string aws_url { get => customEndpoint; }
        public string awsRegion { get => region; }
        public string aws_key{ get => accesskey; }

        public string bucket { get; set; }
        /********************/
    }
}
