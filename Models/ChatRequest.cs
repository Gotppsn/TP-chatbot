namespace AIHelpdeskSupport.Models
{
    public class ChatRequest
    {
        public int ChatbotId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}