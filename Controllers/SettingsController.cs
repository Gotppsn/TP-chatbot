using System.Threading.Tasks;
using AIHelpdeskSupport.Attributes;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdeskSupport.Controllers
{
    [Authorize(Roles = "Admin")]
    [RequirePermission(Permissions.ManageSettings)]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public SettingsController(ISettingsService settingsService, UserManager<ApplicationUser> userManager)
        {
            _settingsService = settingsService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _settingsService.GetSettingsAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGeneral(SystemSettings model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            var settings = await _settingsService.GetSettingsAsync();

            settings.OrganizationName = model.OrganizationName;
            settings.SupportEmail = model.SupportEmail;
            settings.DefaultLanguage = model.DefaultLanguage;
            settings.TimeZone = model.TimeZone;
            settings.DateFormat = model.DateFormat;
            settings.SessionTimeout = model.SessionTimeout;
            settings.RememberSessions = model.RememberSessions;

            var userId = _userManager.GetUserId(User);
            await _settingsService.UpdateSettingsAsync(settings, userId);

            TempData["SuccessMessage"] = "General settings saved successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAppearance(SystemSettings model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            var settings = await _settingsService.GetSettingsAsync();

            settings.Theme = model.Theme;
            settings.AccentColor = model.AccentColor;

            var userId = _userManager.GetUserId(User);
            await _settingsService.UpdateSettingsAsync(settings, userId);

            TempData["SuccessMessage"] = "Appearance settings saved successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveApiSettings(SystemSettings model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            var settings = await _settingsService.GetSettingsAsync();

            settings.FlowiseApiUrl = model.FlowiseApiUrl;
            settings.FlowiseApiKey = model.FlowiseApiKey;

            var userId = _userManager.GetUserId(User);
            await _settingsService.UpdateSettingsAsync(settings, userId);

            TempData["SuccessMessage"] = "API settings saved successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveModelSettings(SystemSettings model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            var settings = await _settingsService.GetSettingsAsync();

            settings.DefaultAiModel = model.DefaultAiModel;
            settings.DefaultTemperature = model.DefaultTemperature;
            settings.DefaultMaxTokens = model.DefaultMaxTokens;

            var userId = _userManager.GetUserId(User);
            await _settingsService.UpdateSettingsAsync(settings, userId);

            TempData["SuccessMessage"] = "Model settings saved successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}