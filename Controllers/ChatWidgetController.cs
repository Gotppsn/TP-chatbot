// Controllers/ChatWidgetController.cs
using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using System.Threading.Tasks;

namespace AIHelpdeskSupport.Controllers
{
    public class ChatWidgetController : Controller
    {
        private readonly IFlowiseService _flowiseService;
        
        public ChatWidgetController(IFlowiseService flowiseService)
        {
            _flowiseService = flowiseService;
        }
        
        public IActionResult Embed(int id)
        {
            // Get chatbot by id
            var chatbot = _flowiseService.GetChatbotByIdAsync(id).Result;
            
            if (chatbot == null)
            {
                return NotFound();
            }
            
            // Configure widget settings based on chatbot
            var settings = new ChatWidgetSettings
            {
                ChatbotId = chatbot.Id,
                WidgetTitle = chatbot.Name,
                WelcomeMessage = $"Hello! I'm the {chatbot.Name} assistant. How can I help you today?",
                PrimaryColor = "#0d6efd" // Default blue
            };
            
            return View(settings);
        }
        
        [HttpPost]
        public IActionResult GenerateEmbedCode(ChatWidgetSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return View("Embed", settings);
            }
            
            // Generate embed code that users can copy
            var embedCode = $@"
<div id=""ai-helpdesk-widget""></div>
<script src=""{Request.Scheme}://{Request.Host}/js/chat-widget.js""></script>
<script>
    window.aiHelpdeskWidget.init({{
        chatbotId: {settings.ChatbotId},
        title: ""{settings.WidgetTitle}"",
        welcomeMessage: ""{settings.WelcomeMessage}"",
        primaryColor: ""{settings.PrimaryColor}"",
        position: ""{settings.Position}"",
        autoOpen: {settings.AutoOpen.ToString().ToLower()},
        showTimestamp: {settings.ShowTimestamp.ToString().ToLower()},
        enableFileUpload: {settings.EnableFileUpload.ToString().ToLower()},
        enableVoiceInput: {settings.EnableVoiceInput.ToString().ToLower()}
    }});
</script>";
            
            ViewBag.EmbedCode = embedCode;
            return View("Embed", settings);
        }
        
        [HttpGet]
        public IActionResult CustomizeWidget(int id)
        {
            // Get chatbot by id
            var chatbot = _flowiseService.GetChatbotByIdAsync(id).Result;
            
            if (chatbot == null)
            {
                return NotFound();
            }
            
            // Initialize settings with defaults
            var settings = new ChatWidgetSettings
            {
                ChatbotId = chatbot.Id,
                WidgetTitle = chatbot.Name,
                WelcomeMessage = $"Hello! I'm the {chatbot.Name} assistant. How can I help you today?"
            };
            
            return View(settings);
        }
        
        [HttpPost]
        public IActionResult CustomizeWidget(ChatWidgetSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return View(settings);
            }
            
            // Save customized widget settings (in a real app, these would be stored in a database)
            TempData["SuccessMessage"] = "Widget settings saved successfully!";
            
            return RedirectToAction("Embed", new { id = settings.ChatbotId });
        }
    }
}

