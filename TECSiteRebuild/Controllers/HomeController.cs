using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TECSite.Models;
using TECSite.EmailService;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Diagnostics;
using Polly;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Net.Http.Headers;
using TECEncryption;

namespace TECSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            ViewData.Add("StatusText", null);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            Console.WriteLine("Home Page"); 
            string domain = Request.Host.Host.Split('.')[0];
            if (domain == "api")
            {
                return RedirectPermanent($"{Program.domain}/Api");
            }

            if (Request.Cookies.ContainsKey("loggedIn"))
            {
                ViewData.Add("uname", Encryption.Decrypt(Request.Cookies["loggedIn"]!, JsonConvert.DeserializeObject<byte[]>(Program.authKey)));
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Privacy()
        {
            Console.WriteLine("Privacy Page");

            string domain = Request.Host.Host.Split('.')[0];
            if (domain == "api")
            {
                return new ContentResult() { Content = $"{Program.domain}/Privacy.pdf", ContentType = "text/plain", StatusCode = 200 };
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Terms()
        {
            Console.WriteLine("Terms and Conditions Page");

            string domain = Request.Host.Host.Split('.')[0];
            if (domain == "api")
            {
                return new ContentResult() { Content = $"{Program.domain}/Terms.pdf", ContentType = "text/plain", StatusCode = 200 };
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Contact(string? email = null, string? usermessage = null, string? usermessagearea = null, IFormFile imageupload = null)
        {
            if (email == null && usermessage == null) { Console.WriteLine("Contact Us Page"); }
            else if (email == null) { ViewData["StatusText"] = "No Email Entered!"; Console.WriteLine("Missing email for contact!"); }
            else if (usermessage == null) { ViewData["StatusText"] = "No Message Entered!"; Console.WriteLine("Missing message for contact!"); }
            else if (usermessagearea != null) { ViewData["StatusText"] = "Bot Lol"; Console.WriteLine("Bot Detected!!!"); }
            else if (imageupload != null && !imageupload.FileName.ToLower().EndsWith(".jpg") &&
                                            !imageupload.FileName.ToLower().EndsWith(".png") &&
                                            !imageupload.FileName.ToLower().EndsWith(".zip") &&
                                            !imageupload.FileName.ToLower().EndsWith(".jpeg"))
            {
                ViewData["StatusText"] = "Incorrect file type submitted! File must be jpg, png, or zip of multiple images"; Console.WriteLine($"\n\nIncorrect File Type submit.\nBEBE{imageupload.FileName}HEA\n\n");
            }
            else
            {
                Console.WriteLine("getting sender");
                EmailSender emailSender = new EmailSender();
                Console.WriteLine("setting email and message");

                Console.WriteLine("setting to dict");
                Dictionary<string, string> nameadressdict = new() { { "TheEnergeticConvention", "staff@thenergeticon.com" } };
                Console.WriteLine("Making Message");
                var message = new Message("WebsiteUser", email, nameadressdict, $"Message from website {email}", $"From: {email}\n{usermessage}", imageupload);
                Console.WriteLine("Sending Message");
                emailSender.SendEmail(message);
                return Redirect($"{Program.domain}/Home");
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult DealersDen()
        {
            Console.WriteLine("Dealers Den Page");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult FAQ()
        {
            Console.WriteLine("FAQ Page");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult PageNotFound()
        {
            Console.WriteLine("Page Not Found!");

            string domain = Request.Host.Host.Split('.')[0];
            if (domain == "api")
            {
                return new NotFoundResult();
            }

            if (Request.Cookies.ContainsKey("loggedIn"))
            {
                ViewData.Add("uname", Encryption.Decrypt(Request.Cookies["loggedIn"]!, JsonConvert.DeserializeObject<byte[]>(Program.authKey)));
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>().Error,  RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
