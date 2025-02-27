// Controllers/UserChatController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using AIHelpdeskSupport.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIHelpdeskSupport.Controllers.Api;
using Microsoft.AspNetCore.Identity;

namespace AIHelpdeskSupport.Controllers
{
    [Authorize(Roles = "User")] // Ensure only users with "User" role can access
    public class UserChatController : Controller

    {
        private readonly IFlowiseService _flowiseService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserChatController(
            IFlowiseService flowiseService,
            UserManager<ApplicationUser> userManager)
        {
            _flowiseService = flowiseService;
            _userManager = userManager;
        }

        // Display departments/chatbots for selection
        public async Task<IActionResult> Index()
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get chatbots and filter by user department
            var chatbots = await _flowiseService.GetAllChatbotsAsync();

            var filteredChatbots = chatbots
                .Where(c => c.IsActive && c.Department == user.Department)
                .GroupBy(c => c.Department)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(filteredChatbots);
        }

        // Display chat interface for a specific chatbot
        // UserChatController.cs - Chat action
        public async Task<IActionResult> Chat(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var chatbot = await _flowiseService.GetChatbotByIdAsync(id);

            if (chatbot == null || !chatbot.IsActive)
            {
                return NotFound();
            }

            // Department authorization check
            if (chatbot.Department != user.Department)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            string sessionId = System.Guid.NewGuid().ToString();

            var viewModel = new UserChatViewModel
            {
                Chatbot = chatbot,
                SessionId = sessionId
            };

            return View(viewModel);
        }

        // API endpoint for sending messages (will be called via AJAX)
        [HttpPost]
        public async Task<IActionResult> SendMessage(int chatbotId, [FromBody] ChatRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var chatbot = await _flowiseService.GetChatbotByIdAsync(chatbotId);
            if (chatbot == null || !chatbot.IsActive || chatbot.Department != user.Department)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            try
            {
                string response = await _flowiseService.GenerateChatResponseAsync(
                    chatbotId,
                    request.Message,
                    request.SessionId ?? System.Guid.NewGuid().ToString());

                return Ok(new { response, sessionId = request.SessionId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", details = ex.Message });
            }
        }
    }
}