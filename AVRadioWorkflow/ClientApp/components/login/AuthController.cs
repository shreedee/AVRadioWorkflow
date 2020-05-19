using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WordPressPCL;

namespace components.login
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        readonly string _wp_url;

        readonly ILogger _logger;

        public AuthController(
            IConfiguration configuration,
            ILogger<AuthController> logger

            )
        {
            _wp_url = configuration["wordpress:url"];
            _logger = logger;
        }

        [HttpPost]
        public async Task<string> getJWT([FromBody]CredsModel creds)
        {
            try
            {
                var client = creatWPClient($"{_wp_url}/wp-json/", _logger);
                client.AuthMethod = WordPressPCL.Models.AuthMethod.JWT;

                await client.RequestJWToken(creds.username, creds.pwd);

                if (!(await client.IsValidJWToken()))
                    throw new bootCommon.ExceptionWithCode("JWT token is not valid");

                return client.GetToken();
            }
            catch (bootCommon.ExceptionWithCode ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new bootCommon.ExceptionWithCode("invalid login credentials", innerException:ex);
            }

        }


        public static WordPressClient creatWPClient(string url, ILogger _logger)
        {
            var client = new WordPressClient(url);
            client.AuthMethod = WordPressPCL.Models.AuthMethod.JWT;

            client.HttpResponsePreProcessing = (res) =>
            {
                _logger.LogInformation($"WP says : {res}");
                Console.WriteLine($"WP says : {res}");
                return res;
            };


            return client;

        }


        public static async Task<WordPressClient>InitWP(string wp_url, Microsoft.AspNetCore.Http.HttpRequest Request, ILogger _logger)
        {
            var accessToken = Request.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new bootCommon.ExceptionWithCode("invalid access token");

            var jwt = accessToken.ToString().Replace("Bearer ", "");
            if (string.IsNullOrWhiteSpace(jwt))
                throw new bootCommon.ExceptionWithCode("invalid login credentials");

            var client = creatWPClient($"{wp_url}/wp-json/", _logger);
            client.AuthMethod = WordPressPCL.Models.AuthMethod.JWT;

            client.SetJWToken(jwt);

            if (!(await client.IsValidJWToken()))
                throw new bootCommon.ExceptionWithCode("invalid login token");

            return client;

        }

        [HttpGet]
        public async Task checkJWT()
        {
            await InitWP(_wp_url, Request, _logger);
        }
    }
}
