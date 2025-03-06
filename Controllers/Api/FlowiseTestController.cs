using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AIHelpdeskSupport.Controllers.Api
{
   [Route("api/test")]
   [ApiController]
   public class FlowiseTestController : ControllerBase
   {
       private readonly IFlowiseService _flowiseService;
       private readonly ISettingsService _settingsService;
       private readonly IConfiguration _configuration;
       private readonly ILogger<FlowiseTestController> _logger;
       
       public FlowiseTestController(
           IFlowiseService flowiseService, 
           ISettingsService settingsService,
           IConfiguration configuration, 
           ILogger<FlowiseTestController> logger)
       {
           _flowiseService = flowiseService;
           _settingsService = settingsService;
           _configuration = configuration;
           _logger = logger;
       }
       
       [HttpGet("flowise")]
       public IActionResult TestFlowiseConfig()
       {
           var apiUrl = _configuration["Flowise:ApiUrl"];
           var apiKey = _configuration["Flowise:ApiKey"];
           
           bool hasApiUrl = !string.IsNullOrEmpty(apiUrl);
           bool hasApiKey = !string.IsNullOrEmpty(apiKey) && apiKey != "your-api-key-here";
           
           return Ok(new { 
               configured = hasApiUrl && hasApiKey,
               apiUrlConfigured = hasApiUrl,
               apiKeyConfigured = hasApiKey,
               apiUrl = apiUrl
           });
       }
       
       [HttpGet("flowise-connection")]
       public async Task<IActionResult> TestFlowiseConnection()
       {
           try
           {
               _logger.LogInformation("Testing Flowise connection with URL: {ApiUrl}", _configuration["Flowise:ApiUrl"]);
               
               bool connectionSuccessful = await _flowiseService.TestFlowiseConnectionAsync();
               
               return Ok(new { 
                   success = connectionSuccessful, 
                   message = connectionSuccessful ? 
                       "Successfully connected to Flowise API" : 
                       "Connection failed to Flowise API",
                   apiUrl = _configuration["Flowise:ApiUrl"],
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error testing Flowise connection");
               return StatusCode(500, new { 
                   success = false, 
                   message = $"Error testing Flowise connection: {ex.Message}",
                   details = ex.ToString(),
                   apiUrl = _configuration["Flowise:ApiUrl"],
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
       }
       
       [HttpGet("chatflows")]
       public async Task<IActionResult> GetChatflows()
       {
           try
           {
               _logger.LogInformation("Fetching chatflows from Flowise API URL: {ApiUrl}", 
                   _configuration["Flowise:ApiUrl"]);
               
               var chatflows = await _flowiseService.GetFlowiseChatflowsAsync();
               
               // Log result
               _logger.LogInformation("Retrieved {Count} chatflows from Flowise API", 
                   chatflows?.Count() ?? 0);
               
               // Check if we actually got data
               if (chatflows == null || !chatflows.Any())
               {
                   return Ok(new { 
                       success = false, 
                       message = "No chatflows found or API connection failed",
                       data = Array.Empty<object>(),
                       apiUrl = _configuration["Flowise:ApiUrl"],
                       timestamp = DateTime.UtcNow.ToString("o")
                   });
               }
               
               return Ok(new { 
                   success = true, 
                   data = chatflows,
                   count = chatflows.Count(),
                   apiUrl = _configuration["Flowise:ApiUrl"],
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error fetching chatflows from Flowise API");
               return StatusCode(500, new { 
                   success = false, 
                   error = ex.Message,
                   innerException = ex.InnerException?.Message,
                   stackTrace = ex.StackTrace,
                   apiUrl = _configuration["Flowise:ApiUrl"],
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
       }
       
       [HttpPost("chat")]
       public async Task<IActionResult> TestChat([FromBody] TestChatRequest request)
       {
           if (string.IsNullOrEmpty(request.Message))
               return BadRequest(new { success = false, message = "Message is required" });
               
           try
           {
               string sessionId = Guid.NewGuid().ToString();
               _logger.LogInformation("Testing chat with ChatbotId={ChatbotId}, Message={Message}", 
                   request.ChatbotId, request.Message);
                   
               string response = await _flowiseService.GenerateChatResponseAsync(
                   request.ChatbotId, 
                   request.Message, 
                   sessionId);
                   
               return Ok(new { 
                   success = true, 
                   response, 
                   sessionId,
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error testing chat request for ChatbotId={ChatbotId}", request.ChatbotId);
               return StatusCode(500, new { 
                   success = false, 
                   error = ex.Message,
                   stackTrace = ex.StackTrace,
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
       }
       
       [HttpGet("config-dump")]
       public IActionResult DumpConfiguration()
       {
           // Security: Only enable in development
           if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
           {
               return NotFound();
           }
           
           return Ok(new {
               flowiseApiUrl = _configuration["Flowise:ApiUrl"],
               flowiseApiKeyExists = !string.IsNullOrEmpty(_configuration["Flowise:ApiKey"]),
               timestamp = DateTime.UtcNow.ToString("o")
           });
       }
       
       [HttpGet("test-url")]
       public async Task<IActionResult> TestUrl([FromQuery] string url)
       {
           try
           {
               if (string.IsNullOrEmpty(url))
                   return BadRequest(new { success = false, message = "URL parameter is required" });
   
               _logger.LogInformation("Testing direct URL: {Url}", url);
               
               using var httpClient = new HttpClient();
               httpClient.Timeout = TimeSpan.FromSeconds(15);
               
               using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
               var response = await httpClient.GetAsync(url, cts.Token);
               
               var content = await response.Content.ReadAsStringAsync();
               string truncatedContent = content.Length > 500 ? content.Substring(0, 500) + "..." : content;
               
               return Ok(new { 
                   success = response.IsSuccessStatusCode,
                   statusCode = (int)response.StatusCode,
                   statusMessage = response.StatusCode.ToString(),
                   contentLength = content.Length,
                   contentPreview = truncatedContent,
                   headers = response.Headers.ToDictionary(h => h.Key, h => h.Value),
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error testing URL {Url}", url);
               return StatusCode(500, new { 
                   success = false, 
                   error = ex.Message,
                   errorType = ex.GetType().Name,
                   url = url,
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
       }
       
       [HttpGet("direct-chatflows")]
       public async Task<IActionResult> TestDirectChatflows([FromQuery] string url, [FromQuery] string apiKey)
       {
           try
           {
               // Use provided URL or get from settings
               if (string.IsNullOrEmpty(url))
               {
                   var settings = await _settingsService.GetSettingsAsync();
                   url = (settings.FlowiseApiUrl ?? _configuration["Flowise:ApiUrl"] ?? "http://localhost:3000/api/") + "chatflows";
               }
               
               // Use provided key or get from settings
               if (string.IsNullOrEmpty(apiKey))
               {
                   var settings = await _settingsService.GetSettingsAsync();
                   apiKey = settings.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
               }
               
               _logger.LogInformation("Testing direct chatflows at URL: {Url} with API key: {KeyProvided}", 
                   url, !string.IsNullOrEmpty(apiKey));
               
               using var httpClient = new HttpClient();
               httpClient.Timeout = TimeSpan.FromSeconds(15);
               
               // Add auth headers if key provided
               if (!string.IsNullOrEmpty(apiKey))
               {
                   httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                   httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
               }
               
               // First try with headers
               var response = await httpClient.GetAsync(url);
               
               // If that fails and we have an API key, try with query parameter
               if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(apiKey))
               {
                   if (url.Contains("?"))
                       response = await httpClient.GetAsync(url + "&apiKey=" + apiKey);
                   else
                       response = await httpClient.GetAsync(url + "?apiKey=" + apiKey);
               }
               
               var content = await response.Content.ReadAsStringAsync();
               
               return Ok(new { 
                   success = response.IsSuccessStatusCode,
                   statusCode = (int)response.StatusCode,
                   statusMessage = response.StatusCode.ToString(),
                   contentLength = content.Length,
                   contentPreview = content.Length > 1000 ? content.Substring(0, 1000) + "..." : content,
                   apiUrl = url,
                   apiKeyProvided = !string.IsNullOrEmpty(apiKey),
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error testing direct chatflows at URL {Url}", url);
               return StatusCode(500, new { 
                   success = false, 
                   error = ex.Message,
                   errorType = ex.GetType().Name,
                   url = url,
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
       }
       
       [HttpGet("test-auth")]
       public async Task<IActionResult> TestAuth([FromQuery] string url, [FromQuery] string apiKey)
       {
           try
           {
               if (string.IsNullOrEmpty(url))
                   return BadRequest(new { success = false, message = "URL parameter required" });
                   
               if (string.IsNullOrEmpty(apiKey))
               {
                   var settings = await _settingsService.GetSettingsAsync();
                   apiKey = settings.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
               }
               
               _logger.LogInformation("Testing auth for URL: {Url} with API key: {KeyProvided}", 
                   url, !string.IsNullOrEmpty(apiKey));
               
               using var httpClient = new HttpClient();
               httpClient.Timeout = TimeSpan.FromSeconds(15);
               
               // Test with 3 different auth methods
               var results = new List<object>();
               
               // Method 1: Headers only
               if (!string.IsNullOrEmpty(apiKey))
               {
                   httpClient.DefaultRequestHeaders.Clear();
                   httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                   httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                   
                   var headersResponse = await httpClient.GetAsync(url);
                   var headersContent = await headersResponse.Content.ReadAsStringAsync();
                   
                   results.Add(new {
                       method = "headers",
                       success = headersResponse.IsSuccessStatusCode,
                       statusCode = (int)headersResponse.StatusCode,
                       contentLength = headersContent.Length
                   });
               }
               
               // Method 2: URL parameters
               if (!string.IsNullOrEmpty(apiKey))
               {
                   httpClient.DefaultRequestHeaders.Clear();
                   
                   string paramUrl = url;
                   if (url.Contains("?"))
                       paramUrl = url + "&apiKey=" + apiKey;
                   else
                       paramUrl = url + "?apiKey=" + apiKey;
                   
                   var paramResponse = await httpClient.GetAsync(paramUrl);
                   var paramContent = await paramResponse.Content.ReadAsStringAsync();
                   
                   results.Add(new {
                       method = "url_parameter",
                       success = paramResponse.IsSuccessStatusCode,
                       statusCode = (int)paramResponse.StatusCode,
                       contentLength = paramContent.Length
                   });
               }
               
               // Method 3: Both
               if (!string.IsNullOrEmpty(apiKey))
               {
                   httpClient.DefaultRequestHeaders.Clear();
                   httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                   httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                   
                   string paramUrl = url;
                   if (url.Contains("?"))
                       paramUrl = url + "&apiKey=" + apiKey;
                   else
                       paramUrl = url + "?apiKey=" + apiKey;
                   
                   var bothResponse = await httpClient.GetAsync(paramUrl);
                   var bothContent = await bothResponse.Content.ReadAsStringAsync();
                   
                   results.Add(new {
                       method = "both",
                       success = bothResponse.IsSuccessStatusCode,
                       statusCode = (int)bothResponse.StatusCode,
                       contentLength = bothContent.Length
                   });
               }
               
               return Ok(new {
                   apiUrl = url,
                   apiKeyProvided = !string.IsNullOrEmpty(apiKey),
                   timestamp = DateTime.UtcNow.ToString("o"),
                   results
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error testing auth for URL {Url}", url);
               return StatusCode(500, new { 
                   success = false, 
                   error = ex.Message,
                   errorType = ex.GetType().Name,
                   url = url,
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
       }
   }

   public class TestChatRequest
   {
       public int ChatbotId { get; set; }
       public string Message { get; set; } = string.Empty;
   }
}