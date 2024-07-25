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

namespace TECSite.Controllers
{
    public class GamesController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public GamesController(ILogger<HomeController> logger)
        {
            _logger = logger;
            ViewData.Add("StatusText", null);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            Console.WriteLine("Games Page"); return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult GTA()
        {
            Console.WriteLine("GTA Page"); return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Rust()
        {
            Console.WriteLine("Rust Page"); return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Minecraft()
        {
            Console.WriteLine("Minecraft Page"); return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult BeamMP()
        {
            Console.WriteLine("BeamMP Page"); return View();
        }
    }
}
