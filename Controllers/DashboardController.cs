// Controllers/DashboardController.cs
using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Services;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AIHelpdeskSupport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IFlowiseService _flowiseService;
        
        public DashboardController(IFlowiseService flowiseService)
        {
            _flowiseService = flowiseService;
        }
        
        public async Task<IActionResult> Index()
        {
            var dashboardViewModel = new DashboardViewModel
            {
                ChatbotCount = 8,
                TotalConversations = 2487,
                AverageResponseTime = 2.4,
                UserSatisfaction = 94,
                
                // Get recent chatbots from service
                Chatbots = await GetActiveChatbotsAsync(),
                
                // Recent activity would typically come from a notification/activity service
                RecentActivities = GetRecentActivities(),
                
                // Metrics for charts - would typically come from analytics service
                ConversationMetrics = GetConversationMetrics(),
                SatisfactionMetrics = GetSatisfactionMetrics()
            };
            
            return View(dashboardViewModel);
        }
        
        private async Task<List<Chatbot>> GetActiveChatbotsAsync()
        {
            try
            {
                // Try to get from service first
                var chatbots = await _flowiseService.GetAllChatbotsAsync();
                return chatbots?.OrderByDescending(c => c.CreatedAt).Take(5).ToList() ?? GetSampleChatbots();
            }
            catch
            {
                // Fallback to sample data
                return GetSampleChatbots();
            }
        }
        
        private List<Chatbot> GetSampleChatbots()
        {
            return new List<Chatbot>
            {
                new Chatbot { Id = 1, Name = "Customer Support Bot", Department = "Customer Service", AiModel = "GPT-4", IsActive = true, Description = "Handles general product inquiries and helps customers troubleshoot common issues." },
                new Chatbot { Id = 2, Name = "IT Helper", Department = "IT Support", AiModel = "Claude 3 Opus", IsActive = true, Description = "Provides technical assistance for internal staff, with expertise in network and software issues." },
                new Chatbot { Id = 3, Name = "Sales Assistant", Department = "Sales", AiModel = "GPT-3.5 Turbo", IsActive = true, Description = "Assists potential customers with product information and helps qualify leads for the sales team." },
                new Chatbot { Id = 4, Name = "Billing Support", Department = "Billing", AiModel = "Claude 3 Sonnet", IsActive = true, Description = "Helps customers with invoice questions, payment processing, and account information." },
                new Chatbot { Id = 5, Name = "Operations Bot", Department = "Operations", AiModel = "GPT-3.5 Turbo", IsActive = false, Description = "Assists internal team with operational tasks and process management (currently undergoing updates)." }
            };
        }
        
        private List<ActivityItem> GetRecentActivities()
        {
            return new List<ActivityItem>
            {
                new ActivityItem { 
                    Message = "Customer Support Bot resolved ticket #1234", 
                    Timestamp = DateTime.Now.AddMinutes(-20), 
                    Type = ActivityType.Success 
                },
                new ActivityItem { 
                    Message = "IT Helper escalated ticket #5678 to human agent", 
                    Timestamp = DateTime.Now.AddMinutes(-60), 
                    Type = ActivityType.Warning 
                },
                new ActivityItem { 
                    Message = "Sales Assistant captured new lead from website chat", 
                    Timestamp = DateTime.Now.AddMinutes(-80), 
                    Type = ActivityType.Info 
                },
                new ActivityItem { 
                    Message = "Billing Support updated payment information for client", 
                    Timestamp = DateTime.Now.AddMinutes(-100), 
                    Type = ActivityType.Primary 
                },
                new ActivityItem { 
                    Message = "System updated AI models for all chatbots", 
                    Timestamp = DateTime.Now.AddMinutes(-140), 
                    Type = ActivityType.Secondary 
                }
            };
        }
        
        private Dictionary<string, List<int>> GetConversationMetrics()
        {
            return new Dictionary<string, List<int>>
            {
                { "Conversations", new List<int> { 324, 386, 475, 412, 498, 390, 502 } },
                { "Resolved", new List<int> { 290, 346, 421, 370, 452, 354, 472 } }
            };
        }
        
        private Dictionary<string, int> GetSatisfactionMetrics()
        {
            return new Dictionary<string, int>
            {
                { "5 Star", 72 },
                { "4 Star", 22 },
                { "3 Star", 4 },
                { "2 Star", 1 },
                { "1 Star", 1 }
            };
        }
    }
}