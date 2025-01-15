using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuntimeEfCoreWeb.Models;
using System.Diagnostics;

namespace RuntimeEfCoreWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        IConfiguration _configuration;
        public HomeController(ILogger<HomeController> logger,IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            DynamicContextExtensions.DynamicContext(connectionString!);

            var entities = DynamicContextExtensions.dynamicContext.Model.GetEntityTypes().ToList();
            return View(entities);
        }

        public IActionResult Privacy()
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
