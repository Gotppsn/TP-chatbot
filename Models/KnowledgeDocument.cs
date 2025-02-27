// Models/KnowledgeDocument.cs
using System.ComponentModel.DataAnnotations;

namespace AIHelpdeskSupport.Models
{
    public enum DocumentType
    {
        Text,
        TextFile,
        PDF,
        Webpage,
        QAPair
    }
    
    public class KnowledgeDocument
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public DocumentType Type { get; set; } = DocumentType.Text;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public string? SourceUrl { get; set; }
        
        public string? FilePath { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string CreatedBy { get; set; } = string.Empty;
        
        public DateTime? LastUpdatedAt { get; set; }
        
        public string? LastUpdatedBy { get; set; }
        
        // Foreign key
        public int KnowledgeBaseId { get; set; }
        
        // Navigation property
        public virtual KnowledgeBase KnowledgeBase { get; set; } = null!;
    }
}
