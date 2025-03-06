using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.Services;

public interface IFlowiseService
{
    Task<IEnumerable<Chatbot>> GetAllChatbotsAsync();
    Task<Chatbot?> GetChatbotByIdAsync(int id);
    Task<Chatbot> CreateChatbotAsync(Chatbot chatbot);
    Task<bool> UpdateChatbotAsync(Chatbot chatbot); // Add this method
    Task<string> GenerateChatResponseAsync(int chatbotId, string message, string sessionId);
    Task GetChatbotByIdAsync(object chatbotId);
    Task<bool> TestFlowiseConnectionAsync();
    Task<IEnumerable<FlowiseChatflow>> GetFlowiseChatflowsAsync();
}