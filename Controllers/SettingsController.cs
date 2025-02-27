// Controllers/SettingsController.cs
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdeskSupport.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}