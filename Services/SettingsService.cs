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
                settings.CreatedBy = userId ?? "System";
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
                settings.FlowiseApiKey = settings.FlowiseApiKey ?? string.Empty;
                settings.SqlServerPassword = settings.SqlServerPassword ?? string.Empty;
                settings.CreatedBy = userId;
                _context.SystemSettings.Add(settings);
                
                // Update connection string cache
                ConnectionStringProvider.UpdateConnectionString(settings);
            }
            else
            {
                // General settings
                existingSettings.OrganizationName = settings.OrganizationName;
                existingSettings.SupportEmail = settings.SupportEmail;
                existingSettings.DefaultLanguage = settings.DefaultLanguage;
                existingSettings.TimeZone = settings.TimeZone;
                existingSettings.DateFormat = settings.DateFormat;
                existingSettings.SessionTimeout = settings.SessionTimeout;
                existingSettings.RememberSessions = settings.RememberSessions;
                existingSettings.Theme = settings.Theme;
                existingSettings.AccentColor = settings.AccentColor;
                
                // Flowise API settings
                existingSettings.FlowiseApiUrl = settings.FlowiseApiUrl;
                existingSettings.FlowiseApiKey = settings.FlowiseApiKey ?? existingSettings.FlowiseApiKey ?? string.Empty;
                
                // SQL Server settings
                existingSettings.SqlServerHost = settings.SqlServerHost;
                existingSettings.SqlServerDatabase = settings.SqlServerDatabase;
                existingSettings.SqlServerUsername = settings.SqlServerUsername;
                existingSettings.SqlServerPassword = settings.SqlServerPassword ?? existingSettings.SqlServerPassword ?? string.Empty;
                existingSettings.SqlServerTrustServerCertificate = settings.SqlServerTrustServerCertificate;
                existingSettings.SqlServerMultipleActiveResultSets = settings.SqlServerMultipleActiveResultSets;
                
                // AI model settings
                existingSettings.DefaultAiModel = settings.DefaultAiModel;
                existingSettings.DefaultTemperature = settings.DefaultTemperature;
                existingSettings.DefaultMaxTokens = settings.DefaultMaxTokens;
                
                // Update metadata
                existingSettings.LastUpdatedAt = DateTime.UtcNow;
                existingSettings.LastUpdatedBy = userId;
                
                // Update connection string cache
                ConnectionStringProvider.UpdateConnectionString(existingSettings);
            }

            await _context.SaveChangesAsync();
        }
    }
}