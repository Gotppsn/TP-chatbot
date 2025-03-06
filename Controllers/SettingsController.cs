using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AIHelpdeskSupport.Attributes;
using AIHelpdeskSupport.Data;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using AIHelpdeskSupport.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace AIHelpdeskSupport.Controllers
{
  [Authorize(Roles = "Admin")]
  [RequirePermission(Permissions.ManageSettings)]
  public class SettingsController : Controller
  {
      private readonly ISettingsService _settingsService;
      private readonly UserManager<ApplicationUser> _userManager;
      private readonly ApplicationDbContext _context;
      private readonly ILogger<SettingsController> _logger;
      private readonly IConfiguration _configuration;
      private readonly IFlowiseService _flowiseService;

      public SettingsController(
          ISettingsService settingsService, 
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context,
          ILogger<SettingsController> logger,
          IConfiguration configuration,
          IFlowiseService flowiseService)
      {
          _settingsService = settingsService;
          _userManager = userManager;
          _context = context;
          _logger = logger;
          _configuration = configuration;
          _flowiseService = flowiseService;
      }

      public async Task<IActionResult> Index()
      {
          var userId = _userManager.GetUserId(User);
          var settings = await _settingsService.GetSettingsAsync(userId);
          
          // Get users and chatbots for department data
          var users = await _userManager.Users.ToListAsync();
          var chatbots = await _context.Chatbots.ToListAsync();
          
          var departments = users
              .Where(u => !string.IsNullOrEmpty(u.Department))
              .GroupBy(u => u.Department)
              .Select(group => new DepartmentViewModel
              {
                  Name = group.Key,
                  UserCount = group.Count(),
                  ChatbotCount = chatbots.Count(c => c.Department == group.Key),
                  CreatedAt = chatbots.Where(c => c.Department == group.Key)
                                     .OrderBy(c => c.CreatedAt)
                                     .FirstOrDefault()?.CreatedAt,
                  CreatedBy = chatbots.Where(c => c.Department == group.Key)
                                     .OrderBy(c => c.CreatedAt)
                                     .FirstOrDefault()?.CreatedBy ?? "System"
              })
              .ToList();
          
          // Add departments that exist only in chatbots but not in users
          var chatbotOnlyDepartments = chatbots
              .Where(c => !string.IsNullOrEmpty(c.Department) && 
                          !departments.Any(d => d.Name == c.Department))
              .GroupBy(c => c.Department)
              .Select(group => new DepartmentViewModel
              {
                  Name = group.Key,
                  UserCount = 0,
                  ChatbotCount = group.Count(),
                  CreatedAt = group.OrderBy(c => c.CreatedAt).FirstOrDefault()?.CreatedAt,
                  CreatedBy = group.OrderBy(c => c.CreatedAt).FirstOrDefault()?.CreatedBy ?? "System"
              });
          
          departments.AddRange(chatbotOnlyDepartments);
          
          ViewBag.Departments = departments.OrderBy(d => d.Name).ToList();
          
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
public async Task<IActionResult> SaveApiSettings(string FlowiseApiUrl, string FlowiseApiKey)
{
    try
    {
        var settings = await _settingsService.GetSettingsAsync();
        
        // Normalize API URL format
        if (!string.IsNullOrEmpty(FlowiseApiUrl))
        {
            FlowiseApiUrl = FlowiseApiUrl.TrimEnd('/') + "/";
        }
        
        settings.FlowiseApiUrl = FlowiseApiUrl;
        settings.FlowiseApiKey = FlowiseApiKey ?? "";
        
        var userId = _userManager.GetUserId(User);
        bool saveResult = await _settingsService.UpdateSettingsAsync(settings, userId);
        
        if (!saveResult)
        {
            return StatusCode(500, new { success = false, message = "Failed to save settings to database." });
        }
        
        // Update runtime configuration
        UpdateConfiguration("Flowise:ApiUrl", settings.FlowiseApiUrl);
        UpdateConfiguration("Flowise:ApiKey", settings.FlowiseApiKey);
        
        // Reset the FlowiseService HTTP client
        _flowiseService.TestFlowiseConnectionAsync().Wait();
        
        return Ok(new 
        { 
            success = true,
            message = "API settings saved successfully.",
            apiUrl = settings.FlowiseApiUrl,
            hasApiKey = !string.IsNullOrEmpty(settings.FlowiseApiKey)
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error saving API settings");
        return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
    }
}

      // Simplified method to update configuration
      private void UpdateConfiguration(string key, string value)
      {
         try
         {
             // Use reflection to update configuration
             var configuration = (IConfigurationRoot)_configuration;
             var memoryConfigProvider = configuration.Providers
                 .FirstOrDefault(p => p.GetType().Name == "MemoryConfigurationProvider");
                 
             if (memoryConfigProvider != null)
             {
                 Type providerType = memoryConfigProvider.GetType();
                 var data = providerType.GetProperty("Data", 
                     System.Reflection.BindingFlags.Instance | 
                     System.Reflection.BindingFlags.NonPublic)?.GetValue(memoryConfigProvider) as IDictionary<string, string>;
                     
                 if (data != null)
                 {
                     data[key] = value;
                     _logger.LogInformation("Updated runtime configuration for {Key}", key);
                 }
             }
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Failed to update runtime configuration for {Key}", key);
         }
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
      public async Task<IActionResult> AddDepartment(string newName)
      {
          if (string.IsNullOrWhiteSpace(newName))
          {
              TempData["ErrorMessage"] = "Department name cannot be empty.";
              return RedirectToAction(nameof(Index));
          }

          // Check if department already exists
          bool departmentExists = await _settingsService.DepartmentExistsAsync(newName);

          if (departmentExists)
          {
              TempData["ErrorMessage"] = $"Department '{newName}' already exists.";
              return RedirectToAction(nameof(Index));
          }

          // Get current user ID
          string userId = _userManager.GetUserId(User);
          
          // Create the department (includes creating default chatbot)
          bool success = await _settingsService.CreateDepartmentAsync(newName, userId);

          if (success)
          {
              TempData["SuccessMessage"] = $"Department '{newName}' has been added successfully.";
          }
          else
          {
              TempData["ErrorMessage"] = $"Failed to add department '{newName}'.";
          }
          
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

          // Skip check if name isn't changing
          if (oldName != newName)
          {
              // Check if new department name already exists
              bool departmentExists = await _settingsService.DepartmentExistsAsync(newName);

              if (departmentExists)
              {
                  TempData["ErrorMessage"] = $"Department '{newName}' already exists.";
                  return RedirectToAction(nameof(Index));
              }
          }

          bool success = await _settingsService.UpdateDepartmentAsync(oldName, newName);

          if (success)
          {
              TempData["SuccessMessage"] = $"Department '{oldName}' has been renamed to '{newName}'.";
          }
          else
          {
              TempData["ErrorMessage"] = $"Failed to update department.";
          }
          
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

          bool success = await _settingsService.DeleteDepartmentAsync(departmentName);

          if (success)
          {
              TempData["SuccessMessage"] = $"Department '{departmentName}' has been deleted.";
          }
          else
          {
              TempData["ErrorMessage"] = $"Failed to delete department '{departmentName}'.";
          }
          
          return RedirectToAction(nameof(Index));
      }
  }
}