// Controllers/Api/ChatController.cs
using AIHelpdeskSupport.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdeskSupport.Controllers.Api;

[Route("api/chat")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly IFlowiseService _flowiseService;
    
    public ChatController(IFlowiseService flowiseService)
    {
        _flowiseService = flowiseService;
    }
    
[HttpPost("{chatbotId}")]
public async Task<IActionResult> SendMessage(int chatbotId, [FromBody] ChatRequest request)
{
    // Generate a session ID if not provided
    string sessionId = request.SessionId ?? Guid.NewGuid().ToString();
    
    string response = await _flowiseService.GenerateChatResponseAsync(
        chatbotId, 
        request.Message, 
        sessionId);
        
    return Ok(new { response, sessionId });
}
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public int ChatbotId { get; internal set; }

}