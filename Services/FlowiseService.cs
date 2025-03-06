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
            
            // Validate URL
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                _logger.LogError("Flowise API URL is empty - API calls will fail");
                apiUrl = "http://localhost:3000/api/";
            }
            
            // Ensure URL ends with a trailing slash
            if (!apiUrl.EndsWith("/"))
            {
                apiUrl += "/";
            }
            
            // Set base address
            _httpClient.BaseAddress = new Uri(apiUrl);
            
            // Clear all existing headers to prevent conflicts
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Add API key in ALL possible formats Flowise might expect
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("apiKey", apiKey);
                
                _logger.LogInformation("Added API authentication headers with key: {KeyPrefix}", 
                    apiKey.Length > 4 ? apiKey.Substring(0, 4) + "..." : "[empty]");
            }
            else
            {
                _logger.LogWarning("No API key provided for Flowise - authentication will likely fail");
            }
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

            // Get API key for direct URL parameter use
            var settings = _context.SystemSettings.FirstOrDefault();
            string apiKey = settings?.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
            
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
            HttpResponseMessage response;
            
            // Try with headers first
            response = await _httpClient.PostAsJsonAsync("prediction", payload, cts.Token);
            
            // If unauthorized, try with query parameter
            if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(apiKey))
            {
                response = await _httpClient.PostAsJsonAsync($"prediction?apiKey={apiKey}", payload, cts.Token);
            }

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
            
            // Get API key for URL parameter
            var settings = _context.SystemSettings.FirstOrDefault();
            string apiKey = settings?.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
            
            // Try the health endpoint first with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            
            // Try the health endpoint with headers first
            var response = await _httpClient.GetAsync("health", cts.Token);
            
            // If that fails with auth error, try with query parameter
            if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(apiKey))
            {
                response = await _httpClient.GetAsync($"health?apiKey={apiKey}", cts.Token);
            }
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Flowise health endpoint check successful");
                return true;
            }
            
            _logger.LogWarning("Flowise health endpoint check failed with status code {StatusCode}", response.StatusCode);
            
            // Try the root endpoint as a fallback
            response = await _httpClient.GetAsync("", cts.Token);
            
            // If that fails with auth error, try with query parameter
            if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(apiKey))
            {
                response = await _httpClient.GetAsync($"?apiKey={apiKey}", cts.Token);
            }
            
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
            
            // Get API key for URL parameter
            var settings = _context.SystemSettings.FirstOrDefault();
            string apiKey = settings?.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
            
            // Log request before sending
            _logger.LogInformation("Requesting chatflows from: {BaseUrl}chatflows", _httpClient.BaseAddress);
            
            // Call the chatflows endpoint with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            // Try with headers first
            var response = await _httpClient.GetAsync("chatflows", cts.Token);
            
            // If that fails with auth error, try with query parameter
            if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(apiKey))
            {
                _logger.LogInformation("Auth failed with headers, trying with URL parameter");
                response = await _httpClient.GetAsync($"chatflows?apiKey={apiKey}", cts.Token);
            }
            
            // If that fails, try v1 endpoint with query param
            if (!response.IsSuccessStatusCode && !string.IsNullOrEmpty(apiKey))
            {
                _logger.LogInformation("Trying v1 endpoint");
                response = await _httpClient.GetAsync($"v1/chatflows?apiKey={apiKey}", cts.Token);
            }
            
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
                else if (doc.RootElement.TryGetProperty("flows", out dataElement) && 
                    dataElement.ValueKind == JsonValueKind.Array)
                {
                    var chatflowsJson = dataElement.GetRawText();
                    var chatflows = JsonSerializer.Deserialize<List<FlowiseChatflow>>(chatflowsJson, options);
                    return chatflows ?? Enumerable.Empty<FlowiseChatflow>();
                }
                else if (doc.RootElement.TryGetProperty("chatflows", out dataElement) && 
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