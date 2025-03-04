// Services/ISettingsService.cs
public interface ISettingsService
{
    Task<SystemSettings> GetSettingsAsync();
    Task UpdateSettingsAsync(SystemSettings settings, string userId);
}

