// Controllers/IntegrationsController.cs
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdeskSupport.Controllers
{
    public class IntegrationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}