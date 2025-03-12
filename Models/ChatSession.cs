// Models/ChatSession.cs
namespace AIHelpdeskSupport.Models
{
public class ChatSession
{
    public string Id { get; set; } = string.Empty;
    public int ChatbotId { get; set; }
    public string? UserId { get; set; } // This is perfect for storing the user identity
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "Active";
    public int? Rating { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public string? Feedback { get; set; }
    public bool IsHidden { get; set; } // Add this line
    public DateTime? HiddenAt { get; set; } // Add this line
    
    // Navigation properties
    public virtual Chatbot Chatbot { get; set; } = null!;
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
}
