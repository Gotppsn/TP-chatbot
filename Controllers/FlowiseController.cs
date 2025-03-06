using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIHelpdeskSupport.Services;
using AIHelpdeskSupport.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace AIHelpdeskSupport.Controllers
{
   [Authorize(Roles = "Admin")]
   [Route("api/flowise")]
   [ApiController]
   public class FlowiseController : ControllerBase
   {
       private readonly IFlowiseService _flowiseService;
       private readonly ISettingsService _settingsService;
       private readonly ILogger<FlowiseController> _logger;

       public FlowiseController(
           IFlowiseService flowiseService,
           ISettingsService settingsService,
           ILogger<FlowiseController> logger)
       {
           _flowiseService = flowiseService;
           _settingsService = settingsService;
           _logger = logger;
       }

       [HttpGet("test-connection")]
       public async Task<IActionResult> TestConnection()
       {
           try
           {
               bool isConnected = await _flowiseService.TestFlowiseConnectionAsync();

               return Ok(new
               {
                   success = isConnected,
                   message = isConnected 
                       ? "Successfully connected to Flowise API" 
                       : "Failed to connect to Flowise API"
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error testing Flowise connection");
               return StatusCode(500, new
               {
                   success = false,
                   message = $"Error: {ex.Message}"
               });
           }
       }

       [HttpGet("chatflows")]
       public async Task<IActionResult> GetChatflows()
       {
           try
           {
               var chatflows = await _flowiseService.GetFlowiseChatflowsAsync();
               
               return Ok(new
               {
                   success = true,
                   data = chatflows,
                   count = chatflows.Count()
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error fetching chatflows");
               return StatusCode(500, new
               {
                   success = false,
                   message = $"Error: {ex.Message}"
               });
           }
       }

       [HttpPost("save-settings")]
       public async Task<IActionResult> SaveSettings([FromBody] FlowiseSettingsDto settings)
       {
           try
           {
               if (string.IsNullOrWhiteSpace(settings.ApiUrl))
               {
                   return BadRequest(new { success = false, message = "API URL is required" });
               }

               var systemSettings = await _settingsService.GetSettingsAsync();
               
               systemSettings.FlowiseApiUrl = settings.ApiUrl.TrimEnd('/') + "/";
               systemSettings.FlowiseApiKey = settings.ApiKey ?? "";
               
               bool success = await _settingsService.UpdateSettingsAsync(systemSettings, User.Identity.Name);
               
               return Ok(new
               {
                   success = success,
                   message = success ? "Settings saved successfully" : "Failed to save settings"
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error saving Flowise settings");
               return StatusCode(500, new
               {
                   success = false,
                   message = $"Error: {ex.Message}"
               });
           }
       }

       [HttpPost("test-chat")]
       public async Task<IActionResult> TestChat([FromBody] TestChatRequest request)
       {
           try
           {
               if (string.IsNullOrEmpty(request.Message))
               {
                   return BadRequest(new { success = false, message = "Message is required" });
               }

               string sessionId = Guid.NewGuid().ToString();
               
               string response = await _flowiseService.GenerateChatResponseAsync(
                   request.ChatbotId,
                   request.Message,
                   sessionId);
               
               return Ok(new
               {
                   success = !string.IsNullOrEmpty(response),
                   response,
                   sessionId
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error testing chat with message: {Message}", request.Message);
               return StatusCode(500, new
               {
                   success = false,
                   message = $"Error: {ex.Message}"
               });
           }
       }
       
       [HttpGet("config")]
       public IActionResult GetConfig()
       {
           // Only available in development
           if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
           {
               return NotFound();
           }
           
           return Ok(new
           {
               apiUrl = _settingsService.GetSettingsAsync().Result.FlowiseApiUrl,
               hasApiKey = !string.IsNullOrEmpty(_settingsService.GetSettingsAsync().Result.FlowiseApiKey)
           });
       }
       
       [HttpPost("assign-chatflow")]
       public async Task<IActionResult> AssignChatflowToChatbot([FromBody] AssignChatflowRequest request)
       {
           try
           {
               var chatbot = await _flowiseService.GetChatbotByIdAsync(request.ChatbotId);
               if (chatbot == null)
               {
                   return NotFound(new { success = false, message = "Chatbot not found" });
               }
               
               chatbot.FlowiseId = request.ChatflowId;
               await _flowiseService.UpdateChatbotAsync(chatbot);
               
               return Ok(new
               {
                   success = true,
                   message = "Chatflow assigned to chatbot successfully"
               });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error assigning chatflow to chatbot");
               return StatusCode(500, new
               {
                   success = false,
                   message = $"Error: {ex.Message}"
               });
           }
       }
   }
   
   public class FlowiseSettingsDto
   {
       public string ApiUrl { get; set; }
       public string ApiKey { get; set; }
   }
   
   public class TestChatRequest
   {
       public int ChatbotId { get; set; }
       public string Message { get; set; }
   }
   
   public class AssignChatflowRequest
   {
       public int ChatbotId { get; set; }
       public string ChatflowId { get; set; }
   }
}