// Controllers/Api/SettingsController.cs
using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Controllers.Api
{
    [Route("api/settings")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public SettingsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("flowise")]
        public async Task<IActionResult> GetFlowiseSettings()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            
            string apiUrl = settings?.FlowiseApiUrl ?? _configuration["Flowise:ApiUrl"] ?? "http://localhost:3000/api/v1/";
            string apiKey = settings?.FlowiseApiKey ?? _configuration["Flowise:ApiKey"] ?? "";
            
            // Ensure URL has trailing slash
            if (!apiUrl.EndsWith("/")) {
                apiUrl += "/";
            }
            
            return Ok(new { 
                flowiseApiUrl = apiUrl,
                flowiseApiKey = apiKey
            });
        }
    }
}