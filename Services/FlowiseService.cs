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
        
        // Normalize URL format
        if (!apiUrl.EndsWith("/"))
            apiUrl += "/";
            
        // If URL doesn't contain /api/, add it
        if (!apiUrl.EndsWith("api/") && !apiUrl.Contains("/api/"))
        {
            apiUrl = apiUrl.TrimEnd('/') + "/api/";
            _logger.LogWarning("API URL didn't contain 'api/' path, corrected to: {ApiUrl}", apiUrl);
        }
        
        // Ensure URL has protocol
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

    private async Task<HttpResponseMessage> TryMultipleEndpoints(string[] endpoints, string apiKey, CancellationToken cancellationToken)
    {
        foreach (var endpoint in endpoints)
        {
            try
            {
                string url = !string.IsNullOrEmpty(apiKey) ? 
                    $"{endpoint}?apiKey={apiKey}" : endpoint;
                
                _logger.LogDebug("Trying endpoint: {Endpoint}", url);
                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully reached endpoint: {Endpoint}", endpoint);
                    return response;
                }
                
                _logger.LogWarning("Endpoint {Endpoint} returned {StatusCode}", 
                    endpoint, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error trying endpoint {Endpoint}: {Error}", endpoint, ex.Message);
            }
        }
        
        throw new InvalidOperationException("Failed to connect to any Flowise API endpoint. Please check your API URL and key.");
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

public async Task<string> GenerateChatResponseAsync(int chatbotId, string message, string sessionId, string language = "en")
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
        
        // Get localized name if available
        string botName = chatbot.Name;
        if (!string.IsNullOrEmpty(chatbot.LocalizedNamesJson) && language != "en")
        {
            try
            {
                var localizedNames = JsonSerializer.Deserialize<Dictionary<string, string>>(chatbot.LocalizedNamesJson);
                if (localizedNames != null && localizedNames.ContainsKey(language))
                {
                    botName = localizedNames[language];
                }
            }
            catch { /* Use default name on error */ }
        }
        
        var payload = new
        {
            question = message,
            sessionId = sessionId,
            overrideConfig = new
            {
                chatId = chatbot.FlowiseId,
                language = language,
                botName = botName
            }
        };

        _logger.LogInformation("Sending request to Flowise API: {Endpoint} with chatId: {ChatId}, language: {Language}", 
            _httpClient.BaseAddress + "prediction", chatbot.FlowiseId, language);

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
        string apiUrl = settings?.FlowiseApiUrl ?? _configuration["Flowise:ApiUrl"] ?? "";
        string apiKey = settings?.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
        
        if (_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("Flowise API URL is not configured.");
        }
        
        // Check if we're using UI URL instead of API URL
        string baseUrl = _httpClient.BaseAddress.ToString();
        if (baseUrl.EndsWith("/") && !baseUrl.EndsWith("api/"))
        {
            // Convert UI URL to API URL
            string apiEndpoint = baseUrl + "api/";
            _logger.LogWarning("Detected UI URL instead of API URL. Using {ApiUrl} instead", apiEndpoint);
            _httpClient.BaseAddress = new Uri(apiEndpoint);
        }
        
        _logger.LogInformation("Requesting chatflows from: {BaseUrl}", _httpClient.BaseAddress);
        
        string[] endpoints = { "chatflows", "api/v1/chatflows", "v1/chatflows" };
        
        HttpResponseMessage response = null;
        string content = null;
        string usedEndpoint = null;
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        
        // Try each endpoint
        foreach (var endpoint in endpoints)
        {
            try
            {
                string url = !string.IsNullOrEmpty(apiKey) ? 
                    $"{endpoint}?apiKey={apiKey}" : endpoint;
                
                _logger.LogInformation("Trying endpoint: {Endpoint}", endpoint);
                response = await _httpClient.GetAsync(url, cts.Token);
                
                content = await response.Content.ReadAsStringAsync();
                
                // Check if we got HTML instead of JSON
                if (content.TrimStart().StartsWith("<!DOCTYPE") || content.TrimStart().StartsWith("<html"))
                {
                    _logger.LogWarning("Received HTML instead of JSON for endpoint {Endpoint}", endpoint);
                    continue;
                }
                
                if (response.IsSuccessStatusCode &&
                    (content.TrimStart().StartsWith("[") || content.TrimStart().StartsWith("{")))
                {
                    usedEndpoint = endpoint;
                    _logger.LogInformation("Success with endpoint: {Endpoint}", endpoint);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error with endpoint {Endpoint}: {Error}", endpoint, ex.Message);
            }
        }
        
        // If we're getting HTML content, we're probably pointed at the UI not the API
        if (content != null && (content.TrimStart().StartsWith("<!DOCTYPE") || content.TrimStart().StartsWith("<html")))
        {
            throw new InvalidOperationException(
                "Received HTML instead of JSON from all endpoints. " +
                "Your API URL is likely pointing to the Flowise UI instead of the API. " +
                "Please update your API URL to include '/api/' at the end, e.g. 'http://localhost:3000/api/'"
            );
        }
        
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(usedEndpoint))
        {
            throw new InvalidOperationException("Could not get a valid JSON response from any endpoint");
        }
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        
        try
        {
            // Direct array format
            if (content.TrimStart().StartsWith("["))
            {
                var chatflows = JsonSerializer.Deserialize<List<FlowiseChatflow>>(content, options);
                _logger.LogInformation("Parsed {Count} chatflows from direct array", chatflows?.Count ?? 0);
                return chatflows ?? new List<FlowiseChatflow>();
            }
            
            // Object with array property
            if (content.TrimStart().StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(content);
                
                // Try common property names
                foreach (var propertyName in new[] { "data", "flows", "chatflows", "result", "results", "items" })
                {
                    if (doc.RootElement.TryGetProperty(propertyName, out JsonElement dataElement) && 
                        dataElement.ValueKind == JsonValueKind.Array)
                    {
                        var chatflowsJson = dataElement.GetRawText();
                        var chatflows = JsonSerializer.Deserialize<List<FlowiseChatflow>>(chatflowsJson, options);
                        _logger.LogInformation("Found {Count} chatflows in '{Property}' property", 
                            chatflows?.Count ?? 0, propertyName);
                        return chatflows ?? new List<FlowiseChatflow>();
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error: {Message}", ex.Message);
        }
        
        throw new FormatException("Could not parse chatflows from response");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching Flowise chatflows: {Message}", ex.Message);
        throw;
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