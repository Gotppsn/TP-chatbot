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
            // Get latest settings from configuration
            string apiUrl = _configuration["Flowise:ApiUrl"] ?? "";
            string apiKey = _configuration["Flowise:ApiKey"] ?? "";
            
            // Validate URL
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                _logger.LogWarning("Flowise API URL is empty");
                apiUrl = "http://localhost:3000/api/";
            }
            
            // Ensure URL ends with a trailing slash
            if (!apiUrl.EndsWith("/"))
            {
                apiUrl += "/";
            }
            
            // Set base address if different
            if (_httpClient.BaseAddress == null || _httpClient.BaseAddress.ToString() != apiUrl)
            {
                _httpClient.BaseAddress = new Uri(apiUrl);
            }
            
            // Clear and set headers
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
            
            // Add API key if provided
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _logger.LogInformation("Added API key to HTTP client headers");
            }
            else
            {
                _logger.LogWarning("No API key provided for Flowise");
            }
            
            _logger.LogInformation("HTTP client configured with URL: {ApiUrl}", apiUrl);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error configuring HTTP client");
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

            // Send request to Flowise with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.PostAsJsonAsync("prediction", payload, cts.Token);

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
            
            // Try the health endpoint first with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            // Try the health endpoint
            var response = await _httpClient.GetAsync("health", cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Flowise health endpoint check successful");
                return true;
            }
            
            _logger.LogWarning("Flowise health endpoint check failed with status code {StatusCode}", response.StatusCode);
            
            // Try the root endpoint as a fallback
            response = await _httpClient.GetAsync("", cts.Token);
            
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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP Request exception during Flowise connection test: {Message}", ex.Message);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout during Flowise connection test");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception during Flowise connection test: {Message}", ex.Message);
            return false;
        }
    }
    
    public async Task<IEnumerable<FlowiseChatflow>> GetFlowiseChatflowsAsync()
    {
        try
        {
            // Always configure HTTP client before each request
            ConfigureHttpClient();
            
            // Log request before sending
            _logger.LogInformation("Requesting chatflows from: {BaseUrl}chatflows", _httpClient.BaseAddress);
            
            // Call the chatflows endpoint with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await _httpClient.GetAsync("chatflows", cts.Token);
            
            // Detailed logging for debugging
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Chatflows response: Status={StatusCode}, Content length={Length}", 
                response.StatusCode, content?.Length ?? 0);
                
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch chatflows: {StatusCode}, Error={Content}", 
                    response.StatusCode, content);
                return Enumerable.Empty<FlowiseChatflow>();
            }
            
            var options = new JsonSerializerOptions { 
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            // Handle both array and object responses
            if (content.StartsWith("["))
            {
                var chatflows = JsonSerializer.Deserialize<List<FlowiseChatflow>>(content, options);
                return chatflows ?? Enumerable.Empty<FlowiseChatflow>();
            }
            else if (content.StartsWith("{"))
            {
                // Some Flowise versions return { data: [...] } format
                using JsonDocument doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) && 
                    dataElement.ValueKind == JsonValueKind.Array)
                {
                    var chatflowsJson = dataElement.GetRawText();
                    var chatflows = JsonSerializer.Deserialize<List<FlowiseChatflow>>(chatflowsJson, options);
                    return chatflows ?? Enumerable.Empty<FlowiseChatflow>();
                }
            }
            
            _logger.LogWarning("Unexpected response format from Flowise API");
            return Enumerable.Empty<FlowiseChatflow>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error fetching Flowise chatflows: {Message}", ex.Message);
            return Enumerable.Empty<FlowiseChatflow>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout while fetching Flowise chatflows");
            return Enumerable.Empty<FlowiseChatflow>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in Flowise chatflows response");
            return Enumerable.Empty<FlowiseChatflow>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching Flowise chatflows: {Message}", ex.Message);
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
    
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}