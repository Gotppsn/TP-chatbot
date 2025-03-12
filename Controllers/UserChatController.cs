using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.ViewModels;
using AIHelpdeskSupport.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModelMessage = AIHelpdeskSupport.Models.ChatMessage;
using ViewModelMessage = AIHelpdeskSupport.ViewModels.ChatMessage;
using AIHelpdeskSupport.Data; // Add this line for ApplicationDbContext
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Controllers
{
    [Authorize] // Ensures only authenticated users can access
    public class UserChatController : Controller
    {
        private readonly IFlowiseService _flowiseService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserChatController> _logger;
        private readonly ApplicationDbContext _context; // Add database context

        public UserChatController(
            IFlowiseService flowiseService, 
            UserManager<ApplicationUser> userManager, 
            ILogger<UserChatController> logger,
            ApplicationDbContext context)
        {
            _flowiseService = flowiseService;
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        // Get all available chatbots for the user
        public async Task<IActionResult> Index()
        {
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

            // Get user's recent chat sessions
            var recentSessions = await _context.ChatSessions
                .Include(s => s.Chatbot)
                .Where(s => s.UserId == currentUser.Id)
                .OrderByDescending(s => s.StartTime)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentSessions = recentSessions;

            return View(filteredChatbots);
        }

        // Start a new chat session
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartChat(int chatbotId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var chatbot = await _flowiseService.GetChatbotByIdAsync(chatbotId);
            if (chatbot == null)
            {
                return NotFound("Chatbot not found");
            }

            // Create a new chat session
            var session = new ChatSession
            {
                Id = Guid.NewGuid().ToString(),
                ChatbotId = chatbotId,
                UserId = currentUser.Id,
                StartTime = DateTime.UtcNow,
                Status = "Active"
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            // Add welcome message
            var welcomeMessage = new ModelMessage
            {
                SessionId = session.Id,
                IsUser = false,
                Content = $"Hello! I'm the {chatbot.Name} assistant. How can I help you today?",
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(welcomeMessage);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ChatSession), new { sessionId = session.Id });
        }

        // API endpoint to send a message
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string sessionId, string message)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "User not authorized" });
            }

            // Get the chat session
            var session = await _context.ChatSessions
                .Include(s => s.Chatbot)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == currentUser.Id);

            if (session == null)
            {
                return NotFound(new { success = false, message = "Chat session not found" });
            }

            if (string.IsNullOrEmpty(message))
            {
                return BadRequest(new { success = false, message = "Message cannot be empty" });
            }

            // Add user message to database
            var userMessage = new ModelMessage
            {
                SessionId = sessionId,
                IsUser = true,
                Content = message,
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(userMessage);
            await _context.SaveChangesAsync();

            // Start timing for response
            var startTime = DateTime.UtcNow;

            // Call AI service to get response
            string response;
            try {
                response = await _flowiseService.GenerateChatResponseAsync(
                    session.ChatbotId,
                    message,
                    sessionId);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error generating chat response");
                response = "Sorry, I'm having trouble processing your request right now.";
            }

            // Add bot response to database
            var botMessage = new ModelMessage
            {
                SessionId = sessionId,
                IsUser = false,
                Content = response,
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(botMessage);
            
            // Update session last activity
            session.LastUpdatedAt = DateTime.UtcNow;
            _context.ChatSessions.Update(session);
            
            await _context.SaveChangesAsync();

            // Calculate response time
            var responseTime = (DateTime.UtcNow - startTime).TotalSeconds;

            return Ok(new
            {
                success = true,
                response = response,
                responseTime = responseTime,
                timestamp = botMessage.Timestamp
            });
        }

        // API endpoint to get chat history
        [HttpGet]
        public async Task<IActionResult> GetChatHistory()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "User not authorized" });
            }

            var sessions = await _context.ChatSessions
                .Include(s => s.Chatbot)
                .Include(s => s.Messages.OrderByDescending(m => m.Timestamp).Take(1))
                .Where(s => s.UserId == currentUser.Id)
                .OrderByDescending(s => s.StartTime)
                .Take(10)
                .Select(s => new ChatHistoryViewModel
                {
                    SessionId = s.Id,
                    ChatbotName = s.Chatbot.Name,
                    Department = s.Chatbot.Department,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    MessageCount = _context.ChatMessages.Count(m => m.SessionId == s.Id),
                    LastMessage = s.Messages.FirstOrDefault().Content,
                    Status = s.Status,
                    Rating = s.Rating,
                    Feedback = s.Feedback
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // API endpoint to clear chat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearChat(string sessionId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "User not authorized" });
            }

            // Get the chat session
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == currentUser.Id);

            if (session == null)
            {
                return NotFound(new { success = false, message = "Chat session not found" });
            }

            // Delete all messages for this session
            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .ToListAsync();

            _context.ChatMessages.RemoveRange(messages);
            
            // Add a new welcome message
            var chatbot = await _flowiseService.GetChatbotByIdAsync(session.ChatbotId);
            var welcomeMessage = new AIHelpdeskSupport.Models.ChatMessage
            {
                SessionId = sessionId,
                IsUser = false,
                Content = $"Hello! I'm the {chatbot.Name} assistant. How can I help you today?",
                Timestamp = DateTime.UtcNow
            };
            
            _context.ChatMessages.Add(welcomeMessage);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Chat cleared successfully" });
        }

        // API endpoint to end chat session
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndChat(string sessionId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "User not authorized" });
            }

            // Get the chat session
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == currentUser.Id);

            if (session == null)
            {
                return NotFound(new { success = false, message = "Chat session not found" });
            }

            // Update session status
            session.Status = "Closed";
            session.EndTime = DateTime.UtcNow;
            
            _context.ChatSessions.Update(session);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Chat ended successfully" });
        }

        // API endpoint to submit feedback

        // Sample chatbots method (unchanged)
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

        [HttpPost]
        public async Task<IActionResult> SendMessage(int chatbotId, string message, string sessionId)
        {
            try
            {
                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not authorized" });
                }

                if (string.IsNullOrEmpty(message))
                {
                    return BadRequest(new { success = false, message = "Message cannot be empty" });
                }

                // Generate sessionId if not provided
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = Guid.NewGuid().ToString();
                }

                // In a real implementation, send message to AI service and get response
                // For demo, just return a simulated response
                string response = GetSimulatedResponse(message, chatbotId);

                return Ok(new
                {
                    success = true,
                    response,
                    sessionId,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return StatusCode(500, new { success = false, message = "An error occurred while processing your message" });
            }
        }

        private string GetSimulatedResponse(string message, int chatbotId)
        {
            // Get chatbot info for context-aware responses
            var chatbot = GetSampleChatbots().FirstOrDefault(c => c.Id == chatbotId);
            string department = chatbot?.Department ?? "Customer Service";

            // Simple response generation based on message content
            string lowercaseMessage = message.ToLower();

            if (lowercaseMessage.Contains("hello") || lowercaseMessage.Contains("hi"))
            {
                return $"Hello! How can I assist you with {department} today?";
            }
            else if (lowercaseMessage.Contains("thank"))
            {
                return "You're welcome! Is there anything else I can help you with?";
            }
            else if (department == "IT Support")
            {
                if (lowercaseMessage.Contains("password") || lowercaseMessage.Contains("reset"))
                {
                    return "To reset your password, please go to the login page and click on 'Forgot Password'. You'll receive an email with instructions to create a new password.";
                }
                else if (lowercaseMessage.Contains("network") || lowercaseMessage.Contains("internet"))
                {
                    return "For network issues, I recommend these steps:\n1. Restart your router\n2. Check if other devices can connect\n3. Try connecting to a different network if possible\n\nIf the problem persists, let me know and I can run a more detailed diagnostic.";
                }
            }
            else if (department == "Sales")
            {
                if (lowercaseMessage.Contains("pricing") || lowercaseMessage.Contains("plan") || lowercaseMessage.Contains("subscription"))
                {
                    return "We offer several subscription plans:\n- Basic: $19/month (1 user, basic features)\n- Professional: $49/month (up to 5 users, all features)\n- Enterprise: $99/month (unlimited users, priority support)\n\nWould you like more details about what each plan includes?";
                }
                else if (lowercaseMessage.Contains("discount") || lowercaseMessage.Contains("offer"))
                {
                    return "We're currently offering a 20% discount for annual subscriptions, and new customers can use the code 'WELCOME15' for 15% off their first three months.";
                }
            }
            else if (department == "Billing")
            {
                if (lowercaseMessage.Contains("invoice") || lowercaseMessage.Contains("receipt"))
                {
                    return "You can find all your invoices in the Billing section of your account dashboard. From there, you can download PDF copies for your records.";
                }
                else if (lowercaseMessage.Contains("payment method") || lowercaseMessage.Contains("update card"))
                {
                    return "To update your payment method, please go to Account Settings > Billing > Payment Methods. You can add new payment methods and set your default option.";
                }
            }

            // Default response if no specific match
            return $"I understand your question about {message}. Let me help you with that. In {department}, we typically handle this by providing detailed information and step-by-step guidance. Could you provide a bit more context so I can give you the most relevant assistance?";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitChatFeedback(string sessionId, int rating, string feedback)
        {
            try
            {
                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not authorized" });
                }

                // Log feedback
                _logger.LogInformation(
                    "Chat feedback submitted - User: {Username}, SessionId: {SessionId}, Rating: {Rating}",
                    currentUser.UserName,
                    sessionId,
                    rating
                );

                // In a real app, you would save feedback to database
                // Here's a simple placeholder for the implementation

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting chat feedback");
                return StatusCode(500, new { success = false, message = "An error occurred while processing your feedback" });
            }
        }

[HttpGet("UserChat/ChatSession/{sessionId}")]
public async Task<IActionResult> ChatSession(string sessionId)
{
    // Move your existing Chat(string sessionId) code here
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null)
    {
        return Challenge();
    }

    // Get the chat session
    var session = await _context.ChatSessions
        .Include(s => s.Chatbot)
        .Include(s => s.Messages.OrderBy(m => m.Timestamp))
        .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == currentUser.Id);

    if (session == null)
    {
        return NotFound("Chat session not found");
    }

    // Create view model
    var viewModel = new UserChatViewModel
    {
        Chatbot = session.Chatbot,
        SessionId = session.Id,
        Messages = session.Messages.Select(m => new ViewModels.ChatMessage
        {
            IsUser = m.IsUser,
            Content = m.Content,
            Timestamp = m.Timestamp
        }).ToList()
    };

    return View("Chat", viewModel);
}

[HttpGet("UserChat/ChatWithBot/{id}")]
public async Task<IActionResult> ChatWithBot(int id)
{
    // Move your existing Chat(int id) code here
    // Try to get chatbot from service with Flowise ID
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

    // Create view model with sample chat messages
    var viewModel = new UserChatViewModel
    {
        Chatbot = chatbot,
        SessionId = Guid.NewGuid().ToString(),
        Messages = new List<ViewModelMessage>
        {
            new ViewModelMessage
            {
                IsUser = false,
                Content = $"👋 Hello! I'm {chatbot.Name}, your AI support agent for {chatbot.Department}. How can I help you today?",
                Timestamp = DateTime.Now.AddMinutes(-5)
            }
        }
    };

    return View("Chat", viewModel);
}
    }
}