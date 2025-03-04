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

            // Send request to Flowise
            var response = await _httpClient.PostAsJsonAsync("prediction", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Flowise API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return $"Error from Flowise API: {response.StatusCode}";
            }

            // Read response
            var result = await response.Content.ReadAsStringAsync();
            return result;
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