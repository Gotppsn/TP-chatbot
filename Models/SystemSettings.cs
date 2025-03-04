// Models/SystemSettings.cs
public class SystemSettings
{
    public int Id { get; set; }
    
    [Required]
    public string OrganizationName { get; set; } = "AI Helpdesk Support";
    
    [Required]
    [EmailAddress]
    public string SupportEmail { get; set; } = "support@example.com";
    
    [Required]
    public string DefaultLanguage { get; set; } = "en";
    
    [Required]
    public string TimeZone { get; set; } = "UTC";
    
    [Required]
    public string DateFormat { get; set; } = "MM/DD/YYYY";
    
    public int SessionTimeout { get; set; } = 30;
    public bool RememberSessions { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string AccentColor { get; set; } = "#0d6efd";
    public string FlowiseApiUrl { get; set; }
    public string FlowiseApiKey { get; set; }
    public string DefaultAiModel { get; set; } = "gpt-3.5-turbo";
    public double DefaultTemperature { get; set; } = 0.7;
    public int DefaultMaxTokens { get; set; } = 1024;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string LastUpdatedBy { get; set; }
}

