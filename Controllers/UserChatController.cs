// Controllers/UserChatController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.ViewModels;
using AIHelpdeskSupport.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIHelpdeskSupport.Controllers
{
    [Authorize(Roles = "User")]
    public class UserChatController : Controller
    {
        private readonly IFlowiseService _flowiseService;

        public UserChatController(IFlowiseService flowiseService)
        {
            _flowiseService = flowiseService;
        }

        public async Task<IActionResult> Index()
        {
            var chatbots = await _flowiseService.GetAllChatbotsAsync();
            return View(chatbots);
        }

        public async Task<IActionResult> Chat(int id)
        {
            var chatbot = await _flowiseService.GetChatbotByIdAsync(id);
            if (chatbot == null)
            {
                return NotFound();
            }

            var viewModel = new UserChatViewModel
            {
                Chatbot = chatbot,
                SessionId = Guid.NewGuid().ToString()
            };

            return View(viewModel);
        }
        public IActionResult History()
        {
            return View();
        }

        public IActionResult Support()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }
    }
}