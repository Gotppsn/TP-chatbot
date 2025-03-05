using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AIHelpdeskSupport.Attributes;
using AIHelpdeskSupport.Data;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Controllers
{
    [Authorize(Roles = "Admin")]
    [RequirePermission(Permissions.ManageSettings)]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public SettingsController(
            ISettingsService settingsService, 
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _settingsService = settingsService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var settings = await _settingsService.GetSettingsAsync(userId);
            
            // Get department data from users and chatbots
            var users = await _userManager.Users.ToListAsync();
            var chatbots = await _context.Chatbots.ToListAsync();
            
            var departments = users
                .Where(u => !string.IsNullOrEmpty(u.Department))
                .GroupBy(u => u.Department)
                .Select(group => new
                {
                    Name = group.Key,
                    UserCount = group.Count(),
                    ChatbotCount = chatbots.Count(c => c.Department == group.Key)
                })
                .OrderBy(d => d.Name)
                .ToList();
            
            ViewBag.Departments = departments;
            
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDepartment(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["ErrorMessage"] = "Department name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            // Check if new department name already exists (and is different from old name)
            if (oldName != newName)
            {
                var departmentExists = await _userManager.Users.AnyAsync(u => u.Department == newName) ||
                                      await _context.Chatbots.AnyAsync(c => c.Department == newName);

                if (departmentExists)
                {
                    TempData["ErrorMessage"] = $"Department '{newName}' already exists.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Update users with the old department name
            var usersToUpdate = await _userManager.Users
                .Where(u => u.Department == oldName)
                .ToListAsync();

            foreach (var user in usersToUpdate)
            {
                user.Department = newName;
                await _userManager.UpdateAsync(user);
                
                // Update the department claim
                var claims = await _userManager.GetClaimsAsync(user);
                var deptClaim = claims.FirstOrDefault(c => c.Type == "Department");
                if (deptClaim != null)
                {
                    await _userManager.RemoveClaimAsync(user, deptClaim);
                    await _userManager.AddClaimAsync(user, new Claim("Department", newName));
                }
            }

            // Update chatbots with the old department name
            var chatbotsToUpdate = await _context.Chatbots
                .Where(c => c.Department == oldName)
                .ToListAsync();

            foreach (var chatbot in chatbotsToUpdate)
            {
                chatbot.Department = newName;
                _context.Update(chatbot);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Department '{oldName}' has been renamed to '{newName}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDepartment(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["ErrorMessage"] = "Department name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            // Check if department already exists
            var departmentExists = await _userManager.Users.AnyAsync(u => u.Department == newName) ||
                                   await _context.Chatbots.AnyAsync(c => c.Department == newName);

            if (departmentExists)
            {
                TempData["ErrorMessage"] = $"Department '{newName}' already exists.";
                return RedirectToAction(nameof(Index));
            }

            // Create a new chatbot for this department
            var chatbot = new Chatbot
            {
                Name = $"{newName} Bot",
                Department = newName,
                AiModel = "gpt-3.5-turbo",
                Description = $"Default chatbot for {newName} department",
                CreatedBy = _userManager.GetUserId(User),
                IsActive = true
            };

            _context.Chatbots.Add(chatbot);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Department '{newName}' has been added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(string departmentName)
        {
            // Check if department is in use by users
            var userCount = await _userManager.Users
                .CountAsync(u => u.Department == departmentName);

            if (userCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete department '{departmentName}' because it is in use by {userCount} users.";
                return RedirectToAction(nameof(Index));
            }

            // Delete chatbots in this department
            var chatbotsToDelete = await _context.Chatbots
                .Where(c => c.Department == departmentName)
                .ToListAsync();

            if (chatbotsToDelete.Any())
            {
                _context.Chatbots.RemoveRange(chatbotsToDelete);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Department '{departmentName}' has been deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}