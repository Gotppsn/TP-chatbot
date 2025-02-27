// Controllers/UserChatController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.ViewModels;
using AIHelpdeskSupport.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIHelpdeskSupport.Controllers
{
    [Authorize(Roles = "User")]
    public class UserChatController : Controller
    {
        private readonly IFlowiseService _flowiseService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserChatController> _logger;

        public UserChatController(IFlowiseService flowiseService, UserManager<ApplicationUser> userManager, ILogger<UserChatController> logger)
        {
            _flowiseService = flowiseService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Get all chatbots
            var allChatbots = await _flowiseService.GetAllChatbotsAsync();

            // If no chatbots found in database, use sample data
            if (allChatbots == null || !allChatbots.Any())
            {
                allChatbots = GetSampleChatbots();
            }

            // Filter chatbots by user's department
            var filteredChatbots = allChatbots.Where(c =>
                c.IsActive &&
                (c.Department == currentUser.Department || c.Department == "General")
            ).ToList();

            return View(filteredChatbots);
        }

        // Sample chatbots for frontend development
        private IEnumerable<Chatbot> GetSampleChatbots()
        {
            return new List<Chatbot>
    {
        new Chatbot { Id = 1, Name = "Customer Support Bot", Department = "Customer Service", AiModel = "GPT-4", IsActive = true, Description = "Handles general product inquiries and helps customers troubleshoot common issues." },
        new Chatbot { Id = 2, Name = "IT Helper", Department = "IT Support", AiModel = "Claude 3 Opus", IsActive = true, Description = "Provides technical assistance for internal staff, with expertise in network and software issues." },
        new Chatbot { Id = 3, Name = "Sales Assistant", Department = "Sales", AiModel = "GPT-3.5 Turbo", IsActive = true, Description = "Assists potential customers with product information and helps qualify leads for the sales team." },
        new Chatbot { Id = 4, Name = "Billing Support", Department = "Billing", AiModel = "Claude 3 Sonnet", IsActive = true, Description = "Helps customers with invoice questions, payment processing, and account information." },
        new Chatbot { Id = 5, Name = "Technical Support", Department = "Technical", AiModel = "GPT-4", IsActive = true, Description = "Provides detailed technical support for complex product issues and integration assistance." },
        new Chatbot { Id = 6, Name = "Operations Bot", Department = "Operations", AiModel = "GPT-3.5 Turbo", IsActive = false, Description = "Assists internal team with operational tasks and process management (currently undergoing updates)." }
    };
        }

        public async Task<IActionResult> Chat(int id)
        {
            // Try to get chatbot from service
            var chatbot = await _flowiseService.GetChatbotByIdAsync(id);

            // If not found, create sample data
            if (chatbot == null)
            {
                chatbot = GetSampleChatbots().FirstOrDefault(c => c.Id == id);
                if (chatbot == null)
                {
                    return NotFound();
                }
            }

            // Create view model
            var viewModel = new UserChatViewModel
            {
                Chatbot = chatbot,
                SessionId = Guid.NewGuid().ToString()
            };

            return View(viewModel);
        }

        // Keep other action methods as they are
        public IActionResult History()
        {
            // Sample data for frontend development
            var chatHistory = new List<ChatHistoryViewModel>
    {
        new ChatHistoryViewModel
        {
            SessionId = "session-1234-abcd",
            ChatbotName = "Customer Support Bot",
            Department = "Customer Service",
            StartTime = DateTime.Now.AddDays(-2),
            EndTime = DateTime.Now.AddDays(-2).AddHours(1),
            MessageCount = 12,
            LastMessage = "Thank you for your help!",
            Status = "Closed",
            Rating = 5,
            Feedback = "The assistant was extremely helpful and resolved my issue quickly."
        },
        new ChatHistoryViewModel
        {
            SessionId = "session-5678-efgh",
            ChatbotName = "IT Helper",
            Department = "IT Support",
            StartTime = DateTime.Now.AddDays(-1),
            EndTime = DateTime.Now.AddDays(-1).AddMinutes(45),
            MessageCount = 8,
            LastMessage = "I'll try that solution, thanks.",
            Status = "Closed",
            Rating = 4,
            Feedback = "Good advice, but took some time to understand my issue."
        },
        new ChatHistoryViewModel
        {
            SessionId = "session-9012-ijkl",
            ChatbotName = "Sales Assistant",
            Department = "Sales",
            StartTime = DateTime.Now.AddHours(-3),
            EndTime = null,
            MessageCount = 5,
            LastMessage = "What are the pricing options?",
            Status = "Active",
            Rating = null,
            Feedback = ""
        },
        new ChatHistoryViewModel
        {
            SessionId = "session-3456-mnop",
            ChatbotName = "Billing Support",
            Department = "Billing",
            StartTime = DateTime.Now.AddDays(-3),
            EndTime = DateTime.Now.AddDays(-3).AddHours(2),
            MessageCount = 15,
            LastMessage = "Your invoice has been updated and sent to your email.",
            Status = "Closed",
            Rating = 3,
            Feedback = "Eventually solved my problem, but the process was complicated."
        },
        new ChatHistoryViewModel
        {
            SessionId = "session-7890-qrst",
            ChatbotName = "Technical Support",
            Department = "Technical",
            StartTime = DateTime.Now.AddHours(-5),
            EndTime = null,
            MessageCount = 7,
            LastMessage = "Could you please provide the error code?",
            Status = "Active",
            Rating = null,
            Feedback = ""
        }
    };

            return View(chatHistory);
        }

        public IActionResult Support()
        {
            return View();
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSupportRequest([FromBody] SupportRequest request)
        {
            try
            {
                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not authorized" });
                }

                // Log support request
                _logger.LogInformation(
                    "Support request submitted - User: {Username}, Category: {Category}, Subject: {Subject}, Priority: {Priority}",
                    currentUser.UserName,
                    request.IssueCategory,
                    request.Subject,
                    request.Priority
                );

                // In a real app, you would save to database and notify IT team
                // Here's a simple placeholder for the implementation

                // Return success response
                return Json(new { success = true, requestId = Guid.NewGuid().ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting support request");
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Your profile has been updated";
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateContact(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Your contact information has been updated";
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "The new password and confirmation password do not match.");
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Your password has been changed";
                //await _signInManager.RefreshSignInAsync(user);
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return RedirectToAction(nameof(Profile));
        }
    }

}