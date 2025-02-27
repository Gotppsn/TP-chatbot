using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using AIHelpdeskSupport.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            // Get user department or default to "Customer Service" if not set
            string userDepartment = !string.IsNullOrEmpty(user.Department) ? user.Department : "Customer Service";

            // Instead of fetching from database, provide sample chatbots for demo
            var chatbots = GetSampleChatbots();

            var filteredChatbots = chatbots
                .Where(c => c.IsActive && c.Department == userDepartment)
                .GroupBy(c => c.Department)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(filteredChatbots);
        }

        // Display chat interface for a specific chatbot
        public async Task<IActionResult> Chat(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get sample chatbots for demo
            var chatbots = GetSampleChatbots();
            var chatbot = chatbots.FirstOrDefault(c => c.Id == id);

            if (chatbot == null || !chatbot.IsActive)
            {
                return NotFound();
            }

            // Department authorization check
            string userDepartment = !string.IsNullOrEmpty(user.Department) ? user.Department : "Customer Service";
            
            if (chatbot.Department != userDepartment)
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
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Get sample chatbots for demo
            var chatbots = GetSampleChatbots();
            var chatbot = chatbots.FirstOrDefault(c => c.Id == request.ChatbotId);
            
            string userDepartment = !string.IsNullOrEmpty(user.Department) ? user.Department : "Customer Service";
            
            if (chatbot == null || !chatbot.IsActive || chatbot.Department != userDepartment)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            try
            {
                // Instead of calling the service, generate sample responses
                string response = GenerateSampleResponse(request.Message, chatbot);

                return Ok(new { response, sessionId = request.SessionId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", details = ex.Message });
            }
        }

        #region Demo Data Methods

        // Sample chatbots for demonstration
        private List<Chatbot> GetSampleChatbots()
        {
            return new List<Chatbot>
            {
                new Chatbot { 
                    Id = 1, 
                    Name = "Customer Support Bot", 
                    Department = "Customer Service", 
                    AiModel = "GPT-4", 
                    IsActive = true, 
                    Description = "Handles general product inquiries and helps customers troubleshoot common issues.",
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    CreatedBy = "Admin"
                },
                new Chatbot { 
                    Id = 2, 
                    Name = "IT Helper", 
                    Department = "IT Support", 
                    AiModel = "Claude 3 Opus", 
                    IsActive = true, 
                    Description = "Provides technical assistance for internal staff, with expertise in network and software issues.",
                    CreatedAt = DateTime.Now.AddMonths(-2),
                    CreatedBy = "Admin"
                },
                new Chatbot { 
                    Id = 3, 
                    Name = "Sales Assistant", 
                    Department = "Sales", 
                    AiModel = "GPT-3.5 Turbo", 
                    IsActive = true, 
                    Description = "Assists potential customers with product information and helps qualify leads for the sales team.",
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    CreatedBy = "Admin"
                },
                new Chatbot { 
                    Id = 4, 
                    Name = "Billing Support", 
                    Department = "Billing", 
                    AiModel = "Claude 3 Sonnet", 
                    IsActive = true, 
                    Description = "Helps customers with invoice questions, payment processing, and account information.",
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    CreatedBy = "Admin"
                },
                new Chatbot { 
                    Id = 5, 
                    Name = "Technical Support", 
                    Department = "IT Support", 
                    AiModel = "GPT-4", 
                    IsActive = true, 
                    Description = "Provides detailed technical support for complex product issues and integration assistance.",
                    CreatedAt = DateTime.Now.AddMonths(-4),
                    CreatedBy = "Admin"
                }
            };
        }

        // Generate sample responses based on department
        private string GenerateSampleResponse(string message, Chatbot chatbot)
        {
            // Basic keyword-based responses based on chatbot department
            string lowercaseMessage = message.ToLower();
            
            // Common greetings
            if (lowercaseMessage.Contains("hello") || lowercaseMessage.Contains("hi") || 
                lowercaseMessage.Contains("hey") || lowercaseMessage.Contains("greetings"))
            {
                return $"Hello! I'm the {chatbot.Name}. How can I help you today?";
            }

            // Farewell messages
            if (lowercaseMessage.Contains("bye") || lowercaseMessage.Contains("goodbye") || 
                lowercaseMessage.Contains("thank you") || lowercaseMessage.Contains("thanks"))
            {
                return "Thank you for chatting with me today! If you have any more questions, feel free to ask anytime.";
            }

            // Department-specific responses
            switch (chatbot.Department)
            {
                case "Customer Service":
                    if (lowercaseMessage.Contains("refund") || lowercaseMessage.Contains("return"))
                    {
                        return "Our refund policy allows returns within 30 days of purchase. Would you like me to help you initiate a return?";
                    }
                    else if (lowercaseMessage.Contains("speak") && (lowercaseMessage.Contains("human") || lowercaseMessage.Contains("agent") || lowercaseMessage.Contains("person")))
                    {
                        return "I'd be happy to connect you with a human agent. Our support team is available Monday-Friday, 9am-5pm EST. Would you like me to create a support ticket for you?";
                    }
                    break;

                case "IT Support":
                    if (lowercaseMessage.Contains("password") || lowercaseMessage.Contains("reset"))
                    {
                        return "To reset your password, please go to the account settings page and click on 'Forgot Password'. You'll receive an email with instructions to create a new password.";
                    }
                    else if (lowercaseMessage.Contains("network") || lowercaseMessage.Contains("wifi") || lowercaseMessage.Contains("internet"))
                    {
                        return "For network connectivity issues, please try restarting your router and ensure all cables are properly connected. If the problem persists, I can help you run some diagnostic tests.";
                    }
                    break;

                case "Sales":
                    if (lowercaseMessage.Contains("price") || lowercaseMessage.Contains("cost") || lowercaseMessage.Contains("pricing"))
                    {
                        return "Our pricing plans start at $19/month for the Basic package, $49/month for Pro, and $99/month for Enterprise. Would you like more details about what each plan includes?";
                    }
                    else if (lowercaseMessage.Contains("discount") || lowercaseMessage.Contains("coupon") || lowercaseMessage.Contains("promo"))
                    {
                        return "We're currently offering a 20% discount for annual subscriptions. I can also provide a special first-time customer discount if you're interested.";
                    }
                    break;

                case "Billing":
                    if (lowercaseMessage.Contains("invoice") || lowercaseMessage.Contains("bill"))
                    {
                        return "You can find all your invoices in the Billing section of your account dashboard. Is there a specific invoice you're looking for?";
                    }
                    else if (lowercaseMessage.Contains("payment") || lowercaseMessage.Contains("method") || lowercaseMessage.Contains("card"))
                    {
                        return "To update your payment method, please go to Account Settings > Billing > Payment Methods. You can add or remove credit cards and set your default payment method there.";
                    }
                    break;
            }

            // Default response if no specific match
            return "I understand you're asking about that. Could you please provide more details so I can better assist you?";
        }

        #endregion
    }

    public class ChatRequest
    {
        public int ChatbotId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}