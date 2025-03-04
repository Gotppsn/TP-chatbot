using System.Threading.Tasks;
using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.Services
{
    public interface ISettingsService
    {
        Task<SystemSettings> GetSettingsAsync(string userId = null);
        Task UpdateSettingsAsync(SystemSettings settings, string userId);
        
    }
}