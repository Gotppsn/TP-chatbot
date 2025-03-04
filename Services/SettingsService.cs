using System;
using System.Threading.Tasks;
using AIHelpdeskSupport.Data;
using AIHelpdeskSupport.Models;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;

        public SettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SystemSettings> GetSettingsAsync(string userId = null)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new SystemSettings();
                settings.CreatedBy = userId ?? "System"; // Use userId if available, otherwise default to "System"
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        public async Task UpdateSettingsAsync(SystemSettings settings, string userId)
        {
            var existingSettings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (existingSettings == null)
            {
                settings.CreatedBy = userId;
                _context.SystemSettings.Add(settings);
            }
            else
            {
                existingSettings.OrganizationName = settings.OrganizationName;
                existingSettings.SupportEmail = settings.SupportEmail;
                existingSettings.DefaultLanguage = settings.DefaultLanguage;
                existingSettings.TimeZone = settings.TimeZone;
                existingSettings.DateFormat = settings.DateFormat;
                existingSettings.SessionTimeout = settings.SessionTimeout;
                existingSettings.RememberSessions = settings.RememberSessions;
                existingSettings.Theme = settings.Theme;
                existingSettings.AccentColor = settings.AccentColor;
                existingSettings.FlowiseApiUrl = settings.FlowiseApiUrl;
                existingSettings.FlowiseApiKey = settings.FlowiseApiKey;
                existingSettings.DefaultAiModel = settings.DefaultAiModel;
                existingSettings.DefaultTemperature = settings.DefaultTemperature;
                existingSettings.DefaultMaxTokens = settings.DefaultMaxTokens;
                existingSettings.LastUpdatedAt = DateTime.UtcNow;
                existingSettings.LastUpdatedBy = userId;
            }

            await _context.SaveChangesAsync();
        }
    }
}