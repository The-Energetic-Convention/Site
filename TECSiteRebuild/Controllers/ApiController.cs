using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TECSite.Models;
using TECSite.EmailService;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;

namespace TECSite.Controllers
{
    public class ApiController : Controller
    {
        private readonly ILogger<ApiController> _logger;

        public ApiController(ILogger<ApiController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            Console.WriteLine("Api Docs Page");

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
