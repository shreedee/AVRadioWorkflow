using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AVRadioWorkflow.Models;
using WordPressPCL;

namespace AVRadioWorkflow.Controllers
{
    public class HomeController : Controller
    {
        /*
        public async Task<IActionResult> Index()
        {
            try
            {
                var client = new WordPressClient("https://www.aurovilleradio.org/wp-json/");

                // Posts
                //var posts = await client.Posts.GetAll();


                //var client = new WordPressClient(ApiCredentials.WordPressUri);
                client.AuthMethod = WordPressPCL.Models.AuthMethod.JWT;
                await client.RequestJWToken(@"Dee", @"oSS0GcVGDF%wv3r)cp*gxyl)");

                // check if authentication has been successful
                var isValidToken = await client.IsValidJWToken();

                var t = await client.GetSettings();

                var g = await client.Posts.GetByID("151602");
            }
            catch (Exception ex)
            {
                var t = ex;
            }

            return View();
        }*/

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
