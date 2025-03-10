using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using AIHelpdeskSupport.Data;

namespace AIHelpdeskSupport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ChatbotController : Controller
    {
        private readonly IFlowiseService _flowiseService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatbotController> _logger;
        private readonly ISettingsService _settingsService;

        public ChatbotController(
            IFlowiseService flowiseService,
            ApplicationDbContext context,
            ILogger<ChatbotController> logger,
            ISettingsService settingsService)
        {
            _flowiseService = flowiseService;
            _context = context;
            _logger = logger;
            _settingsService = settingsService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var chatbots = await _context.Chatbots
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                if (!chatbots.Any())
                {
                    await SyncFlowiseChatbots();
                    chatbots = await _context.Chatbots
                        .OrderByDescending(c => c.CreatedAt)
                        .ToListAsync();
                }

                return View(chatbots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chatbots for Index view");
                return View(new List<Chatbot>());
            }
        }

        public async Task<IActionResult> Create()
        {
            // Get all departments for dropdown
            var departments = await _settingsService.GetAllDepartmentsAsync();
            ViewBag.Departments = departments;
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chatbot chatbot)
        {
            if (ModelState.IsValid)
            {
                chatbot.CreatedAt = DateTime.UtcNow;
                chatbot.CreatedBy = User.Identity.Name ?? "Admin";
                
                // Set default values if not provided
                chatbot.Department ??= "Development";
                chatbot.AiModel ??= "gpt-3.5-turbo";
                
                // Initialize collections if null
                chatbot.Departments ??= new List<string>();
                chatbot.AllowedUsers ??= new List<string>();
                
                // Make sure primary department is in the Departments list
                if (!string.IsNullOrEmpty(chatbot.Department) && !chatbot.Departments.Contains(chatbot.Department))
                {
                    chatbot.Departments.Add(chatbot.Department);
                }
                
                // Set default access type if not provided
                chatbot.AccessType ??= "All";
                
                // Clear allowed users if access type is not Specific
                if (chatbot.AccessType != "Specific")
                {
                    chatbot.AllowedUsers.Clear();
                }
                
                _context.Chatbots.Add(chatbot);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Chatbot created successfully.";
                return RedirectToAction(nameof(Index));
            }
            
            // Get departments again if validation fails
            var departments = await _settingsService.GetAllDepartmentsAsync();
            ViewBag.Departments = departments;
            
            return View(chatbot);
        }

        public async Task<IActionResult> Test(int id)
        {
            var chatbot = await _context.Chatbots.FindAsync(id);
            
            if (chatbot == null)
            {
                return NotFound();
            }

            return View(chatbot);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var chatbot = await _context.Chatbots.FindAsync(id);

                if (chatbot == null)
                {
                    return NotFound();
                }

                // Get all departments for dropdown
                var departments = await _settingsService.GetAllDepartmentsAsync();
                ViewBag.Departments = departments;
                
                // Initialize collections if null
                chatbot.Departments ??= new List<string>();
                chatbot.AllowedUsers ??= new List<string>();
                
                // Make sure primary department is in the Departments list
                if (!string.IsNullOrEmpty(chatbot.Department) && !chatbot.Departments.Contains(chatbot.Department))
                {
                    chatbot.Departments.Add(chatbot.Department);
                }
                
                // Set default access type if not provided
                chatbot.AccessType ??= "All";

                return View(chatbot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chatbot {Id} for edit", id);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Chatbot chatbot)
        {
            if (id != chatbot.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Preserve original creation data
                    var originalChatbot = await _context.Chatbots
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == id);
                        
                    if (originalChatbot != null)
                    {
                        chatbot.CreatedAt = originalChatbot.CreatedAt;
                        chatbot.CreatedBy = originalChatbot.CreatedBy;
                    }
                    
                    // Initialize collections if null
                    chatbot.Departments ??= new List<string>();
                    chatbot.AllowedUsers ??= new List<string>();
                    
                    // Make sure Department is set from Departments list
                    if (chatbot.Departments.Any())
                    {
                        // If Department property isn't in the Departments list,
                        // set Department to first item in Departments list
                        if (string.IsNullOrEmpty(chatbot.Department) || 
                            !chatbot.Departments.Contains(chatbot.Department))
                        {
                            chatbot.Department = chatbot.Departments.First();
                        }
                    }
                    else if (!string.IsNullOrEmpty(chatbot.Department))
                    {
                        // If Departments is empty but Department is set, add it to Departments
                        chatbot.Departments.Add(chatbot.Department);
                    }
                    
                    // Set default access type if not provided
                    chatbot.AccessType ??= "All";
                    
                    // If AccessType is not Specific, clear the AllowedUsers list
                    if (chatbot.AccessType != "Specific")
                    {
                        chatbot.AllowedUsers.Clear();
                    }
                    
                    // Set LastUpdatedAt
                    chatbot.LastUpdatedAt = DateTime.UtcNow;
                    
                    _context.Update(chatbot);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Chatbot updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency error updating chatbot {Id}", id);
                    if (!await ChatbotExists(id))
                    {
                        return NotFound();
                    }
                    
                    ModelState.AddModelError("", "The record was modified by another user.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating chatbot {Id}: {Message}", id, ex.Message);
                    ModelState.AddModelError("", "An error occurred while updating the chatbot.");
                }
            }

            // Get departments again for the view
            var departments = await _settingsService.GetAllDepartmentsAsync();
            ViewBag.Departments = departments;

            return View(chatbot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var chatbot = await _context.Chatbots.FindAsync(id);
                if (chatbot == null)
                {
                    return NotFound();
                }

                _context.Chatbots.Remove(chatbot);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Chatbot deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chatbot {Id}", id);
                TempData["ErrorMessage"] = "Error deleting chatbot.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var chatbot = await _context.Chatbots.FindAsync(id);
                
                if (chatbot == null)
                {
                    return NotFound();
                }

                chatbot.IsActive = !chatbot.IsActive;
                _context.Update(chatbot);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Chatbot {(chatbot.IsActive ? "activated" : "deactivated")} successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for chatbot {Id}", id);
                TempData["ErrorMessage"] = "Error updating chatbot status.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncWithFlowise()
        {
            try
            {
                var result = await SyncFlowiseChatbots();
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    TempData["SyncStats"] = $"Found: {result.TotalFound}, Added: {result.NewCount}, Updated: {result.UpdatedCount}, Unchanged: {result.UnchangedCount}, Skipped: {result.SkippedCount}";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during manual Flowise sync");
                TempData["ErrorMessage"] = "Unexpected error: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<SyncResult> SyncFlowiseChatbots()
        {
            var result = new SyncResult();
            
            try
            {
                var flowiseChatflows = await _flowiseService.GetFlowiseChatflowsAsync();
                
                if (flowiseChatflows == null || !flowiseChatflows.Any())
                {
                    result.Message = "No chatflows found in Flowise API. Check API configuration.";
                    return result;
                }

                result.TotalFound = flowiseChatflows.Count();
                _logger.LogInformation("Found {Count} chatflows in Flowise", result.TotalFound);
                
                foreach (var chatflow in flowiseChatflows)
                {
                    if (string.IsNullOrEmpty(chatflow.Id))
                    {
                        result.SkippedCount++;
                        continue;
                    }
                    
                    var existingChatbot = await _context.Chatbots
                        .FirstOrDefaultAsync(c => c.FlowiseId == chatflow.Id);
                    
                    if (existingChatbot == null)
                    {
                        // Get department settings
                        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
                        string defaultDepartment = "Development";
                        string defaultModel = settings?.DefaultAiModel ?? "gpt-3.5-turbo";
                        
                        // Create new chatbot
                        var newChatbot = new Chatbot
                        {
                            Name = chatflow.Name,
                            Description = "Imported from Flowise - " + DateTime.UtcNow.ToString("yyyy-MM-dd"),
                            FlowiseId = chatflow.Id,
                            Department = defaultDepartment,
                            AiModel = defaultModel,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = "System-Sync",
                            Departments = new List<string> { defaultDepartment },
                            AccessType = "All",
                            AllowedUsers = new List<string>()
                        };
                        
                        _context.Chatbots.Add(newChatbot);
                        result.NewCount++;
                    }
                    else
                    {
                        // Check if any properties need updating
                        bool updated = false;
                        
                        if (existingChatbot.Name != chatflow.Name)
                        {
                            existingChatbot.Name = chatflow.Name;
                            updated = true;
                        }
                        
                        // Initialize collections if null
                        if (existingChatbot.Departments == null)
                        {
                            existingChatbot.Departments = new List<string>();
                            if (!string.IsNullOrEmpty(existingChatbot.Department))
                            {
                                existingChatbot.Departments.Add(existingChatbot.Department);
                            }
                            updated = true;
                        }
                        
                        if (existingChatbot.AllowedUsers == null)
                        {
                            existingChatbot.AllowedUsers = new List<string>();
                            updated = true;
                        }
                        
                        if (string.IsNullOrEmpty(existingChatbot.AccessType))
                        {
                            existingChatbot.AccessType = "All";
                            updated = true;
                        }
                        
                        // Update last sync timestamp
                        existingChatbot.LastUpdatedAt = DateTime.UtcNow;
                        
                        if (updated)
                        {
                            _context.Chatbots.Update(existingChatbot);
                            result.UpdatedCount++;
                        }
                        else
                        {
                            result.UnchangedCount++;
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
                
                result.Success = true;
                result.Message = $"Sync completed: {result.NewCount} new, {result.UpdatedCount} updated, {result.SkippedCount} skipped chatbots";
                _logger.LogInformation(result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                result.Exception = ex;
                _logger.LogError(ex, "Error synchronizing Flowise chatbots");
            }
            
            return result;
        }

        private class SyncResult
        {
            public bool Success { get; set; } = false;
            public int TotalFound { get; set; } = 0;
            public int NewCount { get; set; } = 0;
            public int UpdatedCount { get; set; } = 0;
            public int SkippedCount { get; set; } = 0;
            public int UnchangedCount { get; set; } = 0;
            public string Message { get; set; } = string.Empty;
            public Exception Exception { get; set; } = null;
        }

        private async Task<bool> ChatbotExists(int id)
        {
            return await _context.Chatbots.AnyAsync(c => c.Id == id);
        }
    }
}