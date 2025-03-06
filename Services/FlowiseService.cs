// Services/FlowiseService.cs
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIHelpdeskSupport.Data;
using AIHelpdeskSupport.Models;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Services;

public class FlowiseService : IFlowiseService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FlowiseService> _logger;

    public FlowiseService(
        ApplicationDbContext context,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<FlowiseService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        ConfigureHttpClient();
    }
    
    private void ConfigureHttpClient()
    {
        // Configure the HttpClient with current settings
        string apiUrl = _configuration["Flowise:ApiUrl"] ?? "http://localhost:3000/api/";
        
        // Ensure URL ends with a trailing slash
        if (!string.IsNullOrEmpty(apiUrl) && !apiUrl.EndsWith("/"))
        {
            apiUrl += "/";
        }
        
        _httpClient.BaseAddress = new Uri(apiUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Clear existing auth headers
        if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
        }
        
        if (_httpClient.DefaultRequestHeaders.Contains("x-api-key"))
        {
            _httpClient.DefaultRequestHeaders.Remove("x-api-key");
        }
        
        // Add API key if configured
        string? apiKey = _configuration["Flowise:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }
    }

    public async Task<IEnumerable<Chatbot>> GetAllChatbotsAsync()
    {
        return await _context.Chatbots.ToListAsync();
    }

    public async Task<Chatbot?> GetChatbotByIdAsync(int id)
    {
        return await _context.Chatbots.FindAsync(id);
    }

    public async Task<Chatbot> CreateChatbotAsync(Chatbot chatbot)
    {
        _context.Chatbots.Add(chatbot);
        await _context.SaveChangesAsync();
        return chatbot;
    }

    public async Task<string> GenerateChatResponseAsync(int chatbotId, string message, string sessionId)
    {
        try
        {
            // Refresh HTTP client config to ensure latest settings
            ConfigureHttpClient();
            
            var chatbot = await _context.Chatbots.FindAsync(chatbotId);
            if (chatbot == null)
                return "Chatbot not found";

            if (string.IsNullOrEmpty(chatbot.FlowiseId))
                return "Chatbot has no Flowise ID configured";

            // Create request payload
            var payload = new
            {
                question = message,
                sessionId = sessionId,
                overrideConfig = new
                {
                    chatId = chatbot.FlowiseId
                }
            };

            // Log request for troubleshooting
            _logger.LogInformation("Sending request to Flowise API: {Endpoint} with chatId: {ChatId}", 
                _httpClient.BaseAddress + "prediction", chatbot.FlowiseId);

            // Send request to Flowise
            var response = await _httpClient.PostAsJsonAsync("prediction", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Flowise API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return $"Error from Flowise API: {response.StatusCode} - {errorContent}";
            }

            // Read response
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chat response");
            return $"An error occurred while processing your request: {ex.Message}";
        }
    }

    public async Task GetChatbotByIdAsync(object chatbotId)
    {
        if (chatbotId is int id)
        {
            await GetChatbotByIdAsync(id);
        }
        else if (int.TryParse(chatbotId.ToString(), out int parsedId))
        {
            await GetChatbotByIdAsync(parsedId);
        }
        else
        {
            throw new ArgumentException("chatbotId must be convertible to int", nameof(chatbotId));
        }
    }
    
    public async Task<bool> TestFlowiseConnectionAsync()
    {
        try
        {
            // Update HTTP client configuration to ensure current settings
            ConfigureHttpClient();
            
            _logger.LogInformation("Testing Flowise connection to {BaseAddress}", _httpClient.BaseAddress);
            
            // Try the health endpoint first
            var response = await _httpClient.GetAsync("health");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Flowise health endpoint check successful");
                return true;
            }
            
            _logger.LogWarning("Flowise health endpoint check failed with status code {StatusCode}", response.StatusCode);
            
            // Try the root endpoint as a fallback
            response = await _httpClient.GetAsync("");
            
            bool success = response.IsSuccessStatusCode;
            
            if (success)
            {
                _logger.LogInformation("Flowise root endpoint check successful");
            }
            else
            {
                string content = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Flowise root endpoint check failed: {StatusCode}, Response: {Content}", 
                    response.StatusCode, content);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Flowise connection test");
            return false;
        }
    }
    
    public async Task<IEnumerable<FlowiseChatflow>> GetFlowiseChatflowsAsync()
    {
        try
        {
            ConfigureHttpClient();
            var response = await _httpClient.GetAsync("chatflows");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch chatflows: {StatusCode}", response.StatusCode);
                return Enumerable.Empty<FlowiseChatflow>();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var chatflows = JsonSerializer.Deserialize<List<FlowiseChatflow>>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return chatflows ?? Enumerable.Empty<FlowiseChatflow>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Flowise chatflows");
            return Enumerable.Empty<FlowiseChatflow>();
        }
    }
}

public class FlowiseChatflow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}