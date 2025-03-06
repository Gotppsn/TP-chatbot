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
                    apiUrl = _configuration["Flowise:ApiUrl"]
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Flowise connection");
                return StatusCode(500, new { 
                    success = false, 
                    message = $"Error testing Flowise connection: {ex.Message}",
                    details = ex.ToString() 
                });
            }
        }
        
        [HttpGet("chatflows")]
        public async Task<IActionResult> GetChatflows()
        {
            try
            {
                var chatflows = await _flowiseService.GetFlowiseChatflowsAsync();
                return Ok(new { success = true, data = chatflows });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chatflows");
                return StatusCode(500, new { success = false, error = ex.Message });
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
                string response = await _flowiseService.GenerateChatResponseAsync(
                    request.ChatbotId, 
                    request.Message, 
                    sessionId);
                    
                return Ok(new { success = true, response, sessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing chat request");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    public class TestChatRequest
    {
        public int ChatbotId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}