using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Models;
using System;
using System.Threading.Tasks;

namespace AIHelpdeskSupport.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly ILogger<ChatbotController> _logger;
        
        public ChatbotController(ILogger<ChatbotController> logger)
        {
            _logger = logger;
        }

        [HttpGet("Stats/{id}")]
        public IActionResult GetStats(int id)
        {
            try
            {
                // Return dummy data for now
                return Ok(new {
                    conversations = new Random().Next(10, 100),
                    avgResponseTime = Math.Round(new Random().NextDouble() * 3, 1),
                    successRate = new Random().Next(75, 99)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chatbot stats");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}