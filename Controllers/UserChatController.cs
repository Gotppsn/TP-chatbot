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

        public UserChatController(IFlowiseService flowiseService, UserManager<ApplicationUser> userManager)
        {
            _flowiseService = flowiseService;
            _userManager = userManager;
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
            return View();
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
    }
}