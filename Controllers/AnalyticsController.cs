// Controllers/AnalyticsController.cs
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdeskSupport.Controllers
{
    public class AnalyticsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}