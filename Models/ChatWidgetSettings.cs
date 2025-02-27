// Models/ChatWidgetSettings.cs
namespace AIHelpdeskSupport.Models
{
    public class ChatWidgetSettings
    {
        public int ChatbotId { get; set; }
        public string WidgetTitle { get; set; } = "AI Assistant";
        public string WelcomeMessage { get; set; } = "Hello! How can I help you today?";
        public string Placeholder { get; set; } = "Type your message...";
        public string PrimaryColor { get; set; } = "#0d6efd";
        public string LogoUrl { get; set; } = "";
        public string Position { get; set; } = "right"; // right or left
        public bool AutoOpen { get; set; } = false;
        public bool ShowTimestamp { get; set; } = true;
        public bool EnableFileUpload { get; set; } = false;
        public bool EnableVoiceInput { get; set; } = false;
    }
}