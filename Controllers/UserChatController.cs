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

namespace AIHelpdeskSupport.Controllers
{
    [Authorize(Roles = "User")] // Ensure only users with "User" role can access
    public class UserChatController : Controller
    {
        private readonly IFlowiseService _flowiseService;
        
        public UserChatController(IFlowiseService flowiseService)
        {
            _flowiseService = flowiseService;
        }
        
        // Display departments/chatbots for selection
        public async Task<IActionResult> Index()
        {
            // Get all active chatbots
            var chatbots = await _flowiseService.GetAllChatbotsAsync();
            
            // Filter only active chatbots and group by department
            var chatbotsByDepartment = chatbots
                .Where(c => c.IsActive)
                .GroupBy(c => c.Department)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            return View(chatbotsByDepartment);
        }
        
        // Display chat interface for a specific chatbot
        public async Task<IActionResult> Chat(int id)
        {
            var chatbot = await _flowiseService.GetChatbotByIdAsync(id);
            
            if (chatbot == null || !chatbot.IsActive)
            {
                return NotFound();
            }
            
            // Generate a new session ID for this chat
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
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while processing your request", details = ex.Message });
            }
        }
    }
}