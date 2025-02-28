using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.ViewModels
{
    public class UserChatViewModel
    {
        public Chatbot Chatbot { get; set; } = new Chatbot();
        public string SessionId { get; set; } = string.Empty;
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ChatMessage
    {
        public bool IsUser { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}