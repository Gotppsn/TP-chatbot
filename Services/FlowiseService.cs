using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        try {
            // Get latest settings from database
            var settings = _context.SystemSettings.FirstOrDefault();
            string apiUrl = settings?.FlowiseApiUrl ?? _configuration["Flowise:ApiUrl"] ?? "";
            string apiKey = settings?.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
            
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                _logger.LogError("Flowise API URL is empty - using default http://localhost:3000/api/");
                apiUrl = "http://localhost:3000/api/";
            }
            
            if (!apiUrl.EndsWith("/"))
                apiUrl += "/";
            
            if (!apiUrl.StartsWith("http://") && !apiUrl.StartsWith("https://"))
                apiUrl = "http://" + apiUrl;
            
            _httpClient.BaseAddress = new Uri(apiUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
            
            _logger.LogInformation("HTTP client configured with base URL: {BaseUrl}", apiUrl);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error configuring HTTP client");
        }
    }

    public async Task<IEnumerable<Chatbot>> GetAllChatbotsAsync()
    {
        return await _context.Chatbots
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
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
            ConfigureHttpClient();
            
            var chatbot = await _context.Chatbots.FindAsync(chatbotId);
            if (chatbot == null)
                return "Chatbot not found";

            if (string.IsNullOrEmpty(chatbot.FlowiseId))
                return "Chatbot has no Flowise ID configured";

            var settings = _context.SystemSettings.FirstOrDefault();
            string apiKey = settings?.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
            
            var payload = new
            {
                question = message,
                sessionId = sessionId,
                overrideConfig = new
                {
                    chatId = chatbot.FlowiseId
                }
            };

            _logger.LogInformation("Sending request to Flowise API: {Endpoint} with chatId: {ChatId}", 
                _httpClient.BaseAddress + "prediction", chatbot.FlowiseId);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            HttpResponseMessage response;
            
            response = await _httpClient.PostAsJsonAsync("prediction", payload, cts.Token);
            
            if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(apiKey))
                response = await _httpClient.PostAsJsonAsync($"prediction?apiKey={apiKey}", payload, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Flowise API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return $"Error from Flowise API: {response.StatusCode} - {errorContent}";
            }

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
            await GetChatbotByIdAsync(id);
        else if (int.TryParse(chatbotId.ToString(), out int parsedId))
            await GetChatbotByIdAsync(parsedId);
        else
            throw new ArgumentException("chatbotId must be convertible to int", nameof(chatbotId));
    }
    
    public async Task<bool> TestFlowiseConnectionAsync()
    {
        try
        {
            ConfigureHttpClient();
            
            _logger.LogInformation("Testing Flowise connection to {BaseAddress}", _httpClient.BaseAddress);
            
            var settings = _context.SystemSettings.FirstOrDefault();
            string apiKey = settings?.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            
            var response = await _httpClient.GetAsync("health", cts.Token);
            
            if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(apiKey))
                response = await _httpClient.GetAsync($"health?apiKey={apiKey}", cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Flowise health endpoint check successful");
                return true;
            }
            
            _logger.LogWarning("Flowise health endpoint check failed with status code {StatusCode}", response.StatusCode);
            
            response = await _httpClient.GetAsync("", cts.Token);
            
            if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(apiKey))
                response = await _httpClient.GetAsync($"?apiKey={apiKey}", cts.Token);
            
            bool success = response.IsSuccessStatusCode;
            
            if (success)
                _logger.LogInformation("Flowise root endpoint check successful");
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
            _logger.LogError(ex, "Error testing Flowise connection: {Message}", ex.Message);
            return false;
        }
    }
    
    public async Task<IEnumerable<FlowiseChatflow>> GetFlowiseChatflowsAsync()
    {
        try
        {
            ConfigureHttpClient();
            
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            string apiKey = settings?.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
            
            _logger.LogInformation("Requesting chatflows from: {BaseUrl}chatflows", _httpClient.BaseAddress);
            
            List<string> endpoints = new List<string> { "chatflows", "api/chatflows", "v1/chatflows", "api/v1/chatflows" };
            
            HttpResponseMessage response = null;
            string content = null;
            
            foreach (var endpoint in endpoints)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    
                    string url = !string.IsNullOrEmpty(apiKey) ? 
                        $"{endpoint}?apiKey={apiKey}" : endpoint;
                    
                    _logger.LogInformation("Trying endpoint: {Endpoint}", url);
                    response = await _httpClient.GetAsync(url, cts.Token);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        content = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation("Successful response from {Endpoint} - content length: {Length}", 
                            url, content.Length);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error trying endpoint {Endpoint}: {Error}", endpoint, ex.Message);
                }
            }
            
            if (content == null)
            {
                _logger.LogWarning("Failed to get chatflows from any endpoint");
                return Enumerable.Empty<FlowiseChatflow>();
            }
            
            _logger.LogInformation("Received response: {Content}", 
                content.Length > 100 ? content.Substring(0, 100) + "..." : content);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            if (content.StartsWith("["))
            {
                var chatflows = JsonSerializer.Deserialize<List<FlowiseChatflow>>(content, options);
                _logger.LogInformation("Deserialized {Count} chatflows", chatflows?.Count ?? 0);
                return chatflows ?? Enumerable.Empty<FlowiseChatflow>();
            }
            else if (content.StartsWith("{"))
            {
                using JsonDocument doc = JsonDocument.Parse(content);
                
                foreach (var propertyName in new[] { "data", "flows", "chatflows", "result", "results" })
                {
                    if (doc.RootElement.TryGetProperty(propertyName, out JsonElement dataElement) && 
                        dataElement.ValueKind == JsonValueKind.Array)
                    {
                        var chatflowsJson = dataElement.GetRawText();
                        var chatflows = JsonSerializer.Deserialize<List<FlowiseChatflow>>(chatflowsJson, options);
                        _logger.LogInformation("Deserialized {Count} chatflows from property {Property}", 
                            chatflows?.Count ?? 0, propertyName);
                        return chatflows ?? Enumerable.Empty<FlowiseChatflow>();
                    }
                }
            }
            
            _logger.LogWarning("Could not parse response format: {Content}", 
                content.Substring(0, Math.Min(100, content.Length)));
            return Enumerable.Empty<FlowiseChatflow>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Flowise chatflows: {Message}", ex.Message);
            return Enumerable.Empty<FlowiseChatflow>();
        }
    }

    public async Task<bool> UpdateChatbotAsync(Chatbot chatbot)
    {
        try
        {
            _context.Entry(chatbot).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chatbot {ChatbotId}", chatbot.Id);
            return false;
        }
    }
}

public class FlowiseChatflow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}