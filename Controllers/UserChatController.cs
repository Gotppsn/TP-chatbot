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
            
            // Filter chatbots by user's department
            var filteredChatbots = allChatbots.Where(c => 
                c.IsActive && 
                (c.Department == currentUser.Department || c.Department == "General")
            ).ToList();

            return View(filteredChatbots);
        }

        public async Task<IActionResult> Chat(int id)
        {
            var chatbot = await _flowiseService.GetChatbotByIdAsync(id);
            if (chatbot == null)
            {
                return NotFound();
            }

            // Verify user has access to this chatbot
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            if (chatbot.Department != currentUser.Department && chatbot.Department != "General")
            {
                return Forbid();
            }

            var viewModel = new UserChatViewModel
            {
                Chatbot = chatbot,
                SessionId = System.Guid.NewGuid().ToString()
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