// Models/ChatbotKnowledgeBase.cs
namespace AIHelpdeskSupport.Models
{
    public class ChatbotKnowledgeBase
    {
        public int ChatbotId { get; set; }
        public int KnowledgeBaseId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedBy { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual Chatbot Chatbot { get; set; } = null!;
        public virtual KnowledgeBase KnowledgeBase { get; set; } = null!;
    }
}