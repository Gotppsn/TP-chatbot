using System;
using System.Collections.Generic;
using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.ViewModels
{
    public class DashboardViewModel
    {
        // Summary metrics
        public int ChatbotCount { get; set; }
        public int TotalConversations { get; set; }
        public double AverageResponseTime { get; set; }
        public int UserSatisfaction { get; set; }
        
        // Chatbots
        public List<Chatbot> Chatbots { get; set; } = new List<Chatbot>();
        
        // Recent activities
        public List<ActivityItem> RecentActivities { get; set; } = new List<ActivityItem>();
        
        // Chart data
        public Dictionary<string, List<int>> ConversationMetrics { get; set; } = new Dictionary<string, List<int>>();
        public Dictionary<string, int> SatisfactionMetrics { get; set; } = new Dictionary<string, int>();
    }
    
    public class ActivityItem
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public ActivityType Type { get; set; }
        
        public string FormattedTime => Timestamp.ToString("h:mm tt");
    }
    
    public enum ActivityType
    {
        Primary,
        Success,
        Info,
        Warning,
        Danger,
        Secondary
    }
}