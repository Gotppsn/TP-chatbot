using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Services;
using System;
using System.Linq;
using System.Net.Http;
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
        
        public FlowiseTestController(IFlowiseService flowiseService, IConfiguration configuration)
        {
            _flowiseService = flowiseService;
            _configuration = configuration;
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

        [HttpGet("ping")]
        public async Task<IActionResult> PingFlowise()
        {
            try
            {
                HttpClient client = new HttpClient();
                var apiUrl = _configuration["Flowise:ApiUrl"];
                if (string.IsNullOrEmpty(apiUrl))
                {
                    return BadRequest(new { success = false, error = "Flowise API URL not configured" });
                }
                
                var response = await client.GetAsync(apiUrl);
                
                return Ok(new { 
                    success = response.IsSuccessStatusCode,
                    statusCode = (int)response.StatusCode,
                    reasonPhrase = response.ReasonPhrase
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
        
        [HttpGet("chatflows")]
        public async Task<IActionResult> GetAllChatflows()
        {
            try
            {
                var apiUrl = _configuration["Flowise:ApiUrl"];
                var apiKey = _configuration["Flowise:ApiKey"];
                
                using var client = new HttpClient();
                
                // Fix base URL formatting
                var baseUrl = apiUrl.TrimEnd('/');
                client.BaseAddress = new Uri(baseUrl + "/");
                
                // Add required authorization header
                if (!string.IsNullOrEmpty(apiKey))
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                }
                
                // Request the chatflows
                var response = await client.GetAsync("v1/chatflows");
                string content = await response.Content.ReadAsStringAsync();
                
                return Ok(new { 
                    success = response.IsSuccessStatusCode,
                    statusCode = (int)response.StatusCode,
                    data = content,
                    headers = response.Headers.Select(h => new { h.Key, Value = string.Join(", ", h.Value) })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    error = ex.Message
                });
            }
        }
        
        [HttpGet("auth-debug")]
        public IActionResult DebugAuth()
        {
            var apiKey = _configuration["Flowise:ApiKey"];
            
            return Ok(new {
                keyConfigured = !string.IsNullOrEmpty(apiKey),
                keyFirstChars = !string.IsNullOrEmpty(apiKey) ? apiKey.Substring(0, Math.Min(5, apiKey.Length)) + "..." : null,
                keyLength = apiKey?.Length ?? 0
            });
        }
    }

    public class TestChatRequest
    {
        public int ChatbotId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}