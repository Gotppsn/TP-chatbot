// Models/KnowledgeBase.cs
using System.ComponentModel.DataAnnotations;

namespace AIHelpdeskSupport.Models
{
    public class KnowledgeBase
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string Department { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string CreatedBy { get; set; } = string.Empty;
        
        public DateTime? LastUpdatedAt { get; set; }
        
        public string? LastUpdatedBy { get; set; }
        
        // Navigation properties
        public virtual ICollection<KnowledgeDocument> Documents { get; set; } = new List<KnowledgeDocument>();
    }
}