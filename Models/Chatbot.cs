// Models/Chatbot.cs
using System.ComponentModel.DataAnnotations;

namespace AIHelpdeskSupport.Models;

public class Chatbot
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Department { get; set; } = string.Empty;
    
    [Required]
    public string AiModel { get; set; } = "gpt-3.5-turbo"; // Default model
    
    public string? Description { get; set; }
    
    public string? SystemPrompt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get; set; }
    public List<string> Departments { get; set; } = new List<string>();
    public string AccessType { get; set; } = "public";
    public List<string> AllowedUsers { get; set; } = new List<string>();
    public string? LocalizedNamesJson { get; set; }
    
    
    public virtual ICollection<ChatbotKnowledgeBase> KnowledgeBases { get; set; } = new List<ChatbotKnowledgeBase>();
    
    // Flowise specific configuration
    public string? FlowiseId { get; set; }
    public ICollection<ChatbotUpdate> UpdateHistory { get; set; } = new List<ChatbotUpdate>();
    
}

public class ChatbotUpdate
{
    public int Id { get; set; }
    public int ChatbotId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }
    public string ChangeDescription { get; set; }
    
    // Navigation property
    public virtual Chatbot Chatbot { get; set; }
}