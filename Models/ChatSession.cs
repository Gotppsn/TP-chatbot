// Models/ChatSession.cs
namespace AIHelpdeskSupport.Models
{
    public class ChatSession
    {
        public string Id { get; set; } = string.Empty;
        public int ChatbotId { get; set; }
        public string? UserId { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = "Active";
        public int? Rating { get; set; }
        public string? Feedback { get; set; }
        
        // Navigation properties
        public virtual Chatbot Chatbot { get; set; } = null!;
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}

// Models/ChatMessage.cs
namespace AIHelpdeskSupport.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual ChatSession Session { get; set; } = null!;
    }
}

