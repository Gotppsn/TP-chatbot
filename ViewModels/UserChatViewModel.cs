// ViewModels/UserChatViewModel.cs
using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.ViewModels
{
    public class UserChatViewModel
    {
        public Chatbot Chatbot { get; set; }
        public string SessionId { get; set; }
    }
}