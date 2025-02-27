using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.Services;

public interface IFlowiseService
{
    Task<IEnumerable<Chatbot>> GetAllChatbotsAsync();
    Task<Chatbot?> GetChatbotByIdAsync(int id);
    Task<Chatbot> CreateChatbotAsync(Chatbot chatbot);
    Task<string> GenerateChatResponseAsync(int chatbotId, string message, string sessionId);
}