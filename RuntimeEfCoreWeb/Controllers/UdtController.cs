using Microsoft.AspNetCore.Mvc;

namespace RuntimeEfCoreWeb.Controllers
{


    [Route("Udt/{entityName}")]
    public class UdtController : Controller
    {
        public UdtController(IConfiguration configuration)
        {
            DynamicContextExtensions.DynamicContext(configuration.GetConnectionString("DefaultConnection")!);
        }
        public IActionResult Index(string entityName)
        {

            var items = DynamicContextExtensions.GetEntity(entityName);
            if( items.Count() > 0)
                return View(items);
            return RedirectToAction("Index","Home");
        }
    }
}
