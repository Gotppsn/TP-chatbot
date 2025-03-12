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
using AIHelpdeskSupport.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Controllers
{
    [Authorize] // Ensures only authenticated users can access
    public class UserChatController : Controller
    {
        private readonly IFlowiseService _flowiseService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserChatController> _logger;
        private readonly ApplicationDbContext _context; // Database context

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

        // GET: /UserChat
        // Shows all available chatbots for the user
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

        // POST: /UserChat/StartChat
        // Starts a new chat session with a selected chatbot
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
                Status = "Active",
                LastUpdatedAt = DateTime.UtcNow
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

            return RedirectToAction(nameof(Chat), new { sessionId = session.Id });
        }

[HttpPost]
[ValidateAntiForgeryToken]
[Route("UserChat/SendMessageInSession")]
public async Task<IActionResult> SendMessageInSession([FromBody] ChatMessageRequest request)
{
    _logger.LogInformation("SendMessageInSession called: SessionId={SessionId}, Message={MessageLength}",
        request.SessionId, request.Message?.Length ?? 0);
    
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null)
    {
        _logger.LogWarning("Unauthorized attempt to send message - user not authenticated");
        return Unauthorized(new { success = false, message = "User not authorized" });
    }

    // Get the chat session
    var session = await _context.ChatSessions
        .Include(s => s.Chatbot)
        .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == currentUser.Id);

    if (session == null)
    {
        _logger.LogWarning("Chat session not found: {SessionId}", request.SessionId);
        return NotFound(new { success = false, message = "Chat session not found" });
    }

    if (string.IsNullOrEmpty(request.Message))
    {
        _logger.LogWarning("Empty message received");
        return BadRequest(new { success = false, message = "Message cannot be empty" });
    }

    // Add user message to database
    var userMessage = new ModelMessage
    {
        SessionId = request.SessionId,
        IsUser = true,
        Content = request.Message,
        Timestamp = DateTime.UtcNow
    };

    _context.ChatMessages.Add(userMessage);
    await _context.SaveChangesAsync();

    // Start timing for response
    var startTime = DateTime.UtcNow;

    // Call AI service to get response - INCLUDE LANGUAGE PARAMETER
    string response;
    try {
        _logger.LogInformation("Calling Flowise service for chatbot {ChatbotId}", session.ChatbotId);
        
        // Extract language from request if available (optional parameter)
        string language = request.Language ?? "en";
        
        response = await _flowiseService.GenerateChatResponseAsync(
            session.ChatbotId, 
            request.Message, 
            request.SessionId,
            language);
    }
    catch (Exception ex) {
        _logger.LogError(ex, "Error generating chat response");
        response = "Sorry, I'm having trouble processing your request right now.";
    }

    // Add bot response to database
    var botMessage = new ModelMessage
    {
        SessionId = request.SessionId,
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
    _logger.LogInformation("Response generated in {ResponseTime}s", responseTime);

    return Ok(new
    {
        success = true,
        response = response,
        responseTime = responseTime,
        timestamp = botMessage.Timestamp
    });
}

[HttpGet("Chat/{sessionId}")]
public async Task<IActionResult> Chat(string sessionId)
{
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null)
    {
        return Challenge();
    }

    // Get the chat session with eager loading
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

    return View(viewModel);
}
        // GET: /UserChat/GetChatHistory
        // API endpoint to get chat history for the current user
        [HttpGet]
        [Route("UserChat/GetChatHistory")]
        public async Task<IActionResult> GetChatHistory()
        {
            _logger.LogInformation("GetChatHistory called");
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Unauthorized attempt to get chat history");
                return Unauthorized(new { success = false, message = "User not authorized" });
            }

            _logger.LogInformation("Getting chat history for user {UserId}", currentUser.Id);
            
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

            _logger.LogInformation("Retrieved {Count} chat sessions", sessions.Count);
            
            return Ok(sessions);
        }

        // POST: /UserChat/ClearChat
        // Clears chat history for a session
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("UserChat/ClearChat")]
        public async Task<IActionResult> ClearChat([FromBody] SessionRequest request)
        {
            _logger.LogInformation("ClearChat called: SessionId={SessionId}", request.SessionId);
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "User not authorized" });
            }

            // Get the chat session
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == currentUser.Id);

            if (session == null)
            {
                return NotFound(new { success = false, message = "Chat session not found" });
            }

            // Delete all messages for this session
            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == request.SessionId)
                .ToListAsync();

            _context.ChatMessages.RemoveRange(messages);
            
            // Add a new welcome message
            var chatbot = await _flowiseService.GetChatbotByIdAsync(session.ChatbotId);
            var welcomeMessage = new AIHelpdeskSupport.Models.ChatMessage
            {
                SessionId = request.SessionId,
                IsUser = false,
                Content = $"Hello! I'm the {chatbot.Name} assistant. How can I help you today?",
                Timestamp = DateTime.UtcNow
            };
            
            _context.ChatMessages.Add(welcomeMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Chat cleared successfully: SessionId={SessionId}", request.SessionId);
            
            return Ok(new { success = true, message = "Chat cleared successfully" });
        }

        // POST: /UserChat/EndChat
        // Ends a chat session
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("UserChat/EndChat")]
        public async Task<IActionResult> EndChat([FromBody] SessionRequest request)
        {
            _logger.LogInformation("EndChat called: SessionId={SessionId}", request.SessionId);
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "User not authorized" });
            }

            // Get the chat session
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == currentUser.Id);

            if (session == null)
            {
                return NotFound(new { success = false, message = "Chat session not found" });
            }

            // Update session status
            session.Status = "Closed";
            session.EndTime = DateTime.UtcNow;
            
            _context.ChatSessions.Update(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Chat ended successfully: SessionId={SessionId}", request.SessionId);
            
            return Ok(new { success = true, message = "Chat ended successfully" });
        }

        // POST: /UserChat/SubmitChatFeedback
        // Submits feedback for a chat session
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("UserChat/SubmitChatFeedback")]
        public async Task<IActionResult> SubmitChatFeedback([FromBody] FeedbackRequest request)
        {
            _logger.LogInformation("SubmitChatFeedback called: SessionId={SessionId}, Rating={Rating}", 
                request.SessionId, request.Rating);
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "User not authorized" });
            }

            try
            {
                // Get the session
                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == currentUser.Id);
                
                if (session == null)
                {
                    return NotFound(new { success = false, message = "Chat session not found" });
                }
                
                // Update feedback
                session.Rating = request.Rating;
                session.Feedback = request.Feedback;
                
                _context.ChatSessions.Update(session);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Feedback submitted successfully: SessionId={SessionId}", request.SessionId);
                
                return Ok(new { success = true, message = "Feedback submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting chat feedback");
                return StatusCode(500, new { success = false, message = "An error occurred while processing your feedback" });
            }
        }

        // GET: /UserChat/History
        // View chat history
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

        // GET: /UserChat/Support
        // Support page
        public IActionResult Support()
        {
            return View();
        }

        // GET: /UserChat/Profile
        // User profile page
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        // POST: /UserChat/SubmitSupportRequest
        // Submits a support request
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("UserChat/SubmitSupportRequest")]
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

        // POST: /UserChat/UpdateProfile
        // Updates user profile information
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

        // POST: /UserChat/UpdateContact
        // Updates user contact information
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

        // POST: /UserChat/ChangePassword
        // Changes user password
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

        // Helper method to get sample chatbots
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
    }

    // Helper classes for request handling
    public class ChatMessageRequest
    {
        public string SessionId { get; set; }
        public string Message { get; set; }
        public string Language { get; set; }
    }

    public class SessionRequest
    {
        public string SessionId { get; set; }
    }

    public class FeedbackRequest
    {
        public string SessionId { get; set; }
        public int Rating { get; set; }
        public string Feedback { get; set; }
    }
}