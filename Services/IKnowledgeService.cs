// Services/IKnowledgeService.cs
using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.Services
{
    public interface IKnowledgeService
    {
        Task<IEnumerable<KnowledgeBase>> GetAllKnowledgeBasesAsync();
        Task<KnowledgeBase?> GetKnowledgeBaseByIdAsync(int id);
        Task<KnowledgeBase> CreateKnowledgeBaseAsync(KnowledgeBase knowledgeBase);
        Task UpdateKnowledgeBaseAsync(KnowledgeBase knowledgeBase);
        Task DeleteKnowledgeBaseAsync(int id);
        
        Task<IEnumerable<KnowledgeDocument>> GetDocumentsByKnowledgeBaseIdAsync(int knowledgeBaseId);
        Task<KnowledgeDocument?> GetDocumentByIdAsync(int id);
        Task<KnowledgeDocument> AddDocumentAsync(KnowledgeDocument document);
        Task UpdateDocumentAsync(KnowledgeDocument document);
        Task DeleteDocumentAsync(int id);
        
        Task<string> ProcessFileAsync(IFormFile file);
        Task<bool> AssignKnowledgeBaseToBot(int knowledgeBaseId, int chatbotId);
    }
}