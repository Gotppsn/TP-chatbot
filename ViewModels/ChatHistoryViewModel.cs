// ViewModels/ChatHistoryViewModel.cs
using System;

namespace AIHelpdeskSupport.ViewModels
{
    public class ChatHistoryViewModel
    {
        public string SessionId { get; set; } = string.Empty;
        public string ChatbotName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int MessageCount { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Active" or "Closed"
        public int? Rating { get; set; } // 1-5 stars
        public string Feedback { get; set; } = string.Empty;
    }
}