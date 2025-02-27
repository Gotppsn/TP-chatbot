// Models/SupportRequest.cs
namespace AIHelpdeskSupport.Models
{
    public class SupportRequest
    {
        public string IssueCategory { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public bool NotifyEmail { get; set; }
    }
}