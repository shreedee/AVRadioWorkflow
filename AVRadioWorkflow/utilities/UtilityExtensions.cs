using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;

namespace CustomExtensions
{
    public  static partial class UtilityExtensions
    {

        public static string ReplaceInBegining(this string input,string pattern,  string toReplace)
        {
            if (!input.StartsWith(pattern))
                return input;

            var restOfIt = input.Substring(pattern.Length);

            return toReplace+ restOfIt;
        }

        public static string GetUserId(this IHttpContextAccessor httpContextAccessor)
        {
            return GetUserId(httpContextAccessor.HttpContext);
        }

        public static string GetUserId(this HttpContext httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
                return null;

            return httpContext.User.Identity.Name;

            //return httpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
        }


        /// <summary>
        /// a Trick to know what is my current URL. We figure out origin by removing the known URL part
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="currentKnownRoute">the router from calling action</param>
        /// <returns></returns>
        public static string originFromURL(this ControllerBase controller, string currentKnownRoute)
        {

            //[7] = {[Referer, https://coolme.testrev.com:8079/]}
            var refererVals = controller.Request.Headers["Referer"];
            var referer = refererVals.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(referer))
            {
                referer = controller.Request.Headers["origin"].ToArray().FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(referer))
            {
                var refUrl = new Uri(referer);

                var ret = $"{ refUrl.Scheme}://{refUrl.Host}";
                if (refUrl.Port != 80)
                    ret = $"{ret}:{refUrl.Port}";
                return ret.TrimEnd('/');
            }


            //http://localhost:56393/api/nativeInstaller/5b13fa4759716837787eca96
            var rawURl = Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(controller.Request);

            //changing a bit here
            var myindex = rawURl.IndexOf(currentKnownRoute);

            //if not found we need to fix our reverse proxy
            if (-1 != myindex)
            {
                //                throw new Exception($"invalid forwarding for {currentKnownRoute}");
                var myOrigin = rawURl.Substring(0, myindex);
                myOrigin = myOrigin.TrimEnd(new[] { '/' });
                return myOrigin;
            }

            var fromHost = $"{ controller.Request.Scheme}://{controller.Request.Host}";

            return fromHost;

        }

        /// <summary>
        /// core comtabale extension for FromXmlString
        /// </summary>
        /// <param name="rsa"></param>
        /// <param name="xmlString"></param>
        public static void FromXmlStringCore(this RSACryptoServiceProvider rsa, string xmlString)
        {
            RSAParameters parameters = new RSAParameters();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus": parameters.Modulus = Convert.FromBase64String(node.InnerText); break;
                        case "Exponent": parameters.Exponent = Convert.FromBase64String(node.InnerText); break;
                        case "P": parameters.P = Convert.FromBase64String(node.InnerText); break;
                        case "Q": parameters.Q = Convert.FromBase64String(node.InnerText); break;
                        case "DP": parameters.DP = Convert.FromBase64String(node.InnerText); break;
                        case "DQ": parameters.DQ = Convert.FromBase64String(node.InnerText); break;
                        case "InverseQ": parameters.InverseQ = Convert.FromBase64String(node.InnerText); break;
                        case "D": parameters.D = Convert.FromBase64String(node.InnerText); break;
                    }
                }
            }
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            rsa.ImportParameters(parameters);
        }

        /// <summary>
        /// core comtabale extension for ToXmlString
        /// </summary>
        /// <param name="rsa"></param>
        /// <returns></returns>
        public static string ToXmlStringCore(this RSACryptoServiceProvider rsa, bool includePrivateParameters)
        {
            RSAParameters parameters = rsa.ExportParameters(includePrivateParameters);

            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
                Convert.ToBase64String(parameters.Modulus),
                Convert.ToBase64String(parameters.Exponent),
                Convert.ToBase64String(parameters.P),
                Convert.ToBase64String(parameters.Q),
                Convert.ToBase64String(parameters.DP),
                Convert.ToBase64String(parameters.DQ),
                Convert.ToBase64String(parameters.InverseQ),
                Convert.ToBase64String(parameters.D));
        }
    }
}
