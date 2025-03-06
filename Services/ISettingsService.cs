using AIHelpdeskSupport.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIHelpdeskSupport.Services
{
    public interface ISettingsService
    {
        Task<SystemSettings> GetSettingsAsync(string userId = null);
        Task<bool> UpdateSettingsAsync(SystemSettings settings, string userId);
        Task<List<string>> GetAllDepartmentsAsync();
        Task<bool> DepartmentExistsAsync(string departmentName);
        Task<bool> CreateDepartmentAsync(string name, string userId);
        Task<bool> UpdateDepartmentAsync(string oldName, string newName);
        Task<bool> DeleteDepartmentAsync(string name);
    }
}