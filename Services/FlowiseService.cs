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

        // Configure the HttpClient
        string apiUrl = _configuration["Flowise:ApiUrl"] ?? "http://localhost:3000/api/";
        _httpClient.BaseAddress = new Uri(apiUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add API key if configured
        string? apiKey = _configuration["Flowise:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
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
        // For now, just save the chatbot without Flowise integration
        _context.Chatbots.Add(chatbot);
        await _context.SaveChangesAsync();
        return chatbot;
    }

public async Task<string> GenerateChatResponseAsync(int chatbotId, string message, string sessionId)
{
    try
    {
        var chatbot = await _context.Chatbots.FindAsync(chatbotId);
        if (chatbot == null) return "Chatbot not found";

        // Get Flowise chatflow ID
        string flowiseId = chatbot.FlowiseId ?? _configuration["Flowise:DefaultChatflow"];
        if (string.IsNullOrEmpty(flowiseId)) return "No Flowise chatflow ID configured";

        // Prepare request
        var requestData = new
        {
            question = message,
            sessionId = sessionId,
            chatId = chatbot.Id.ToString(),
            overrideConfig = new { chatHistory = true }
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(requestData),
            Encoding.UTF8,
            "application/json");

        // Call Flowise API
        var response = await _httpClient.PostAsync($"prediction/{flowiseId}", content);
        
        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync();
            
            using (JsonDocument doc = JsonDocument.Parse(responseJson))
            {
                // Extract text - adjust path based on Flowise response structure
                if (doc.RootElement.TryGetProperty("text", out JsonElement textElement))
                {
                    return textElement.GetString() ?? "No response text found";
                }
                else if (doc.RootElement.TryGetProperty("result", out JsonElement resultElement))
                {
                    if (resultElement.ValueKind == JsonValueKind.String)
                    {
                        return resultElement.GetString() ?? "No response text found";
                    }
                }
                
                return responseJson;
            }
        }
        
        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogError("Flowise API error: {ErrorContent}", errorContent);
        return $"Error from Flowise API: {response.StatusCode}";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating chat response");
        return "An error occurred while processing your request.";
    }
}

    public async Task GetChatbotByIdAsync(object chatbotId)
    {
        // Convert the object to int if possible
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
}