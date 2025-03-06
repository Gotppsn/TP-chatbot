// Controllers/Api/FlowiseTestController.cs
using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Services;
using System;
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
       private readonly IConfiguration _configuration;
       private readonly ILogger<FlowiseTestController> _logger;
       
       public FlowiseTestController(IFlowiseService flowiseService, IConfiguration configuration, ILogger<FlowiseTestController> logger)
       {
           _flowiseService = flowiseService;
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
   }

   public class TestChatRequest
   {
       public int ChatbotId { get; set; }
       public string Message { get; set; } = string.Empty;
   }
}