// Services/KnowledgeService.cs
using AIHelpdeskSupport.Data;
using AIHelpdeskSupport.Models;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Services
{
    public class KnowledgeService : IKnowledgeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<KnowledgeService> _logger;
        
        public KnowledgeService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<KnowledgeService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }
        
        public async Task<IEnumerable<KnowledgeBase>> GetAllKnowledgeBasesAsync()
        {
            return await _context.KnowledgeBases
                .Include(k => k.Documents)
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<KnowledgeBase?> GetKnowledgeBaseByIdAsync(int id)
        {
            return await _context.KnowledgeBases
                .Include(k => k.Documents)
                .FirstOrDefaultAsync(k => k.Id == id);
        }
        
        public async Task<KnowledgeBase> CreateKnowledgeBaseAsync(KnowledgeBase knowledgeBase)
        {
            _context.KnowledgeBases.Add(knowledgeBase);
            await _context.SaveChangesAsync();
            return knowledgeBase;
        }
        
        public async Task UpdateKnowledgeBaseAsync(KnowledgeBase knowledgeBase)
        {
            _context.Entry(knowledgeBase).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        
        public async Task DeleteKnowledgeBaseAsync(int id)
        {
            var knowledgeBase = await _context.KnowledgeBases.FindAsync(id);
            
            if (knowledgeBase != null)
            {
                _context.KnowledgeBases.Remove(knowledgeBase);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task<IEnumerable<KnowledgeDocument>> GetDocumentsByKnowledgeBaseIdAsync(int knowledgeBaseId)
        {
            return await _context.KnowledgeDocuments
                .Where(d => d.KnowledgeBaseId == knowledgeBaseId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<KnowledgeDocument?> GetDocumentByIdAsync(int id)
        {
            return await _context.KnowledgeDocuments.FindAsync(id);
        }
        
        public async Task<KnowledgeDocument> AddDocumentAsync(KnowledgeDocument document)
        {
            _context.KnowledgeDocuments.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }
        
        public async Task UpdateDocumentAsync(KnowledgeDocument document)
        {
            _context.Entry(document).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        
        public async Task DeleteDocumentAsync(int id)
        {
            var document = await _context.KnowledgeDocuments.FindAsync(id);
            
            if (document != null)
            {
                // If document has a file, delete it from storage
                if (!string.IsNullOrEmpty(document.FilePath) && File.Exists(document.FilePath))
                {
                    File.Delete(document.FilePath);
                }
                
                _context.KnowledgeDocuments.Remove(document);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task<string> ProcessFileAsync(IFormFile file)
        {
            try
            {
                // Create uploads directory if it doesn't exist
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "knowledge");
                Directory.CreateDirectory(uploadsFolder);
                
                // Generate unique filename
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                // Save file to disk
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                
                // Return the relative path
                return Path.Combine("uploads", "knowledge", uniqueFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload");
                throw;
            }
        }
        
        public async Task<bool> AssignKnowledgeBaseToBot(int knowledgeBaseId, int chatbotId)
        {
            // In a real implementation, this would create an association between
            // a chatbot and a knowledge base, possibly in a separate join table
            
            // For this example, we just validate that both exist
            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            var chatbot = await _context.Chatbots.FindAsync(chatbotId);
            
            if (knowledgeBase == null || chatbot == null)
            {
                return false;
            }
            
            // In a real implementation, we would create/update the association here
            
            return true;
        }
    }
}