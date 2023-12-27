using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TECSite.Models;
using TECSite.EmailService;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace TECSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            Console.WriteLine("Home Page");

            string domain = Request.Host.Host.Split('.')[0];
            if (domain == "api")
            {
                return RedirectPermanent($"{Program.domain}/Api");
            }

            return View();
        }

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

        public IActionResult Contact(string? email = null, string? usermessage = null)
        {
            if (email == null && usermessage == null) { Console.WriteLine("Contact Us Page"); }
            else if (email == null) { Console.WriteLine("Missing email for contact!"); }
            else if (usermessage == null) { Console.WriteLine("Missing message for contact!"); }
            else
            {
                Console.WriteLine("getting sender");
                EmailSender emailSender = new EmailSender();
                Console.WriteLine("setting email and message");

                Console.WriteLine("setting to dict");
                Dictionary<string, string> nameadressdict = new() { { "TheEnergeticConvention", "staff@thenergeticon.com" } };
                Console.WriteLine("Making Message");
                var message = new Message("WebsiteUser", email, nameadressdict, "Message from website", $"From: {email}\n{usermessage}", null);
                Console.WriteLine("Sending Message");
                emailSender.SendEmail(message);
                return Redirect($"{Program.domain}/Home");
            }
            return View();
        }

        public IActionResult DealersDen()
        {
            Console.WriteLine("Dealers Den Page");
            return View();
        }

        public IActionResult FAQ()
        {
            Console.WriteLine("FAQ Page");
            return View();
        }

        public IActionResult PageNotFound()
        {
            Console.WriteLine("Page Not Found!");

            string domain = Request.Host.Host.Split('.')[0];
            if (domain == "api")
            {
                return new NotFoundResult();
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
