using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        public AuthController(IConfiguration configuration)
        {
            _wp_url = configuration["wordpress:url"];
        }

        [HttpPost]
        public async Task<string> getJWT([FromBody]CredsModel creds)
        {
            try
            {
                var client = new WordPressClient($"{_wp_url}/wp-json/");
                client.AuthMethod = WordPressPCL.Models.AuthMethod.JWT;
                await client.RequestJWToken(creds.username, creds.pwd);

                if (!(await client.IsValidJWToken()))
                    throw new Exception("invalid token");

                return client.GetToken();
            }
            catch(Exception ex)
            {
                throw new bootCommon.ExceptionWithCode("invalid login credentials", innerException:ex);
            }

        }

        public static async Task<WordPressClient>InitWP(string wp_url, Microsoft.AspNetCore.Http.HttpRequest Request)
        {
            var accessToken = Request.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new bootCommon.ExceptionWithCode("invalid access token");

            var jwt = accessToken.ToString().Replace("Bearer ", "");
            if (string.IsNullOrWhiteSpace(jwt))
                throw new bootCommon.ExceptionWithCode("invalid login credentials");

            var client = new WordPressClient($"{wp_url}/wp-json/");
            client.AuthMethod = WordPressPCL.Models.AuthMethod.JWT;

            client.SetJWToken(jwt);

            if (!(await client.IsValidJWToken()))
                throw new bootCommon.ExceptionWithCode("invalid login credentials");

            return client;

        }

        [HttpGet]
        public async Task checkJWT()
        {
            await InitWP(_wp_url, Request);
        }
    }
}
