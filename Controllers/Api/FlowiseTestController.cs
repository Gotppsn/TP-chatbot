using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
                var apiUrl = _configuration["Flowise:ApiUrl"];
                var apiKey = _configuration["Flowise:ApiKey"];
                
                if (string.IsNullOrEmpty(apiUrl))
                {
                    return BadRequest(new { success = false, message = "Flowise API URL not configured" });
                }
                
                using (var client = new HttpClient())
                {
                    // Set up the client
                    client.BaseAddress = new Uri(apiUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    
                    // Add authentication header if API key is available
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                    }
                    
                    // Try to access the health endpoint or any public endpoint
                    var response = await client.GetAsync("health");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        return Ok(new { 
                            success = true, 
                            message = "Successfully connected to Flowise API",
                            statusCode = (int)response.StatusCode
                        });
                    }
                    
                    // If that failed, try the root endpoint
                    response = await client.GetAsync("");
                    
                    // Get response content for error details
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    return Ok(new { 
                        success = response.IsSuccessStatusCode, 
                        message = response.IsSuccessStatusCode ? 
                            "Connected to Flowise API" : 
                            $"Connection failed with status: {response.StatusCode}",
                        statusCode = (int)response.StatusCode,
                        details = responseContent
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Flowise connection");
                return BadRequest(new { 
                    success = false, 
                    message = $"Error testing Flowise connection: {ex.Message}",
                    error = ex.ToString()
                });
            }
        }
        
        [HttpPost("chat")]
        public async Task<IActionResult> TestChat([FromBody] TestChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
                return BadRequest("Message is required");
                
            string sessionId = Guid.NewGuid().ToString();
            string response = await _flowiseService.GenerateChatResponseAsync(
                request.ChatbotId, 
                request.Message, 
                sessionId);
                
            return Ok(new { response, sessionId });
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