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
               // Log configuration for debugging
               _logger.LogInformation("Using Flowise configuration: URL={ApiUrl}, Key present={KeyPresent}",
                   _configuration["Flowise:ApiUrl"], 
                   !string.IsNullOrEmpty(_configuration["Flowise:ApiKey"]));
                   
               var chatflows = await _flowiseService.GetFlowiseChatflowsAsync();
               
               // Log the result
               _logger.LogInformation("Retrieved {Count} chatflows", chatflows?.Count() ?? 0);
               
               return Ok(new { 
                   success = true, 
                   data = chatflows,
                   apiUrl = _configuration["Flowise:ApiUrl"],
                   timestamp = DateTime.UtcNow.ToString("o")
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error fetching chatflows");
               return StatusCode(500, new { 
                   success = false, 
                   error = ex.Message,
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
   }

   public class TestChatRequest
   {
       public int ChatbotId { get; set; }
       public string Message { get; set; } = string.Empty;
   }
}