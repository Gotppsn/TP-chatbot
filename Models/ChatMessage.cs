// Models/ChatMessage.cs
namespace AIHelpdeskSupport.Models{
public class ChatMessage
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsVisible { get; set; } = true;
    
    // Navigation property
    public virtual ChatSession Session { get; set; } = null!;
}}

