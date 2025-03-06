using AIHelpdeskSupport.Data;
using AIHelpdeskSupport.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIHelpdeskSupport.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(ApplicationDbContext context, ILogger<SettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SystemSettings> GetSettingsAsync(string userId = null)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                // Create default settings if not exists
                settings = new SystemSettings
                {
                    OrganizationName = "AI Helpdesk Support",
                    SupportEmail = "support@example.com",
                    DefaultLanguage = "en",
                    TimeZone = "UTC",
                    DateFormat = "MM/DD/YYYY",
                    CreatedBy = userId ?? "System"
                };

                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        // In Services/SettingsService.cs
        public async Task<bool> UpdateSettingsAsync(SystemSettings settings, string userId)
        {
            try
            {
                // Get existing settings with tracking enabled
                var existingSettings = await _context.SystemSettings.FirstOrDefaultAsync();

                if (existingSettings == null)
                {
                    // Create new settings if none exist
                    settings.CreatedAt = DateTime.UtcNow;
                    settings.CreatedBy = userId ?? "System";
                    settings.LastUpdatedAt = DateTime.UtcNow;
                    settings.LastUpdatedBy = userId ?? "System";
                    _context.SystemSettings.Add(settings);
                }
                else
                {
                    // Explicitly update each property
                    existingSettings.OrganizationName = settings.OrganizationName;
                    existingSettings.SupportEmail = settings.SupportEmail;
                    existingSettings.DefaultLanguage = settings.DefaultLanguage;
                    existingSettings.TimeZone = settings.TimeZone;
                    existingSettings.DateFormat = settings.DateFormat;
                    existingSettings.SessionTimeout = settings.SessionTimeout;
                    existingSettings.RememberSessions = settings.RememberSessions;
                    existingSettings.Theme = settings.Theme;
                    existingSettings.AccentColor = settings.AccentColor;
                    existingSettings.DefaultAiModel = settings.DefaultAiModel;
                    existingSettings.DefaultTemperature = settings.DefaultTemperature;
                    existingSettings.DefaultMaxTokens = settings.DefaultMaxTokens;
                    existingSettings.FlowiseApiUrl = settings.FlowiseApiUrl;
                    existingSettings.FlowiseApiKey = settings.FlowiseApiKey;
                    existingSettings.LastUpdatedAt = DateTime.UtcNow;
                    existingSettings.LastUpdatedBy = userId ?? "System";
                    // Update tracked entity
                    _context.SystemSettings.Update(existingSettings);
                }

                // Explicitly save changes with logging
                int rowsAffected = await _context.SaveChangesAsync();
                _logger.LogInformation("SystemSettings updated with {RowsAffected} rows affected", rowsAffected);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system settings: {ErrorMessage}", ex.Message);
                return false;
            }
        }

        public async Task<List<string>> GetAllDepartmentsAsync()
        {
            // Get unique departments from users and chatbots
            var userDepartments = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.Department))
                .Select(u => u.Department)
                .Distinct()
                .ToListAsync();

            var chatbotDepartments = await _context.Chatbots
                .Where(c => !string.IsNullOrEmpty(c.Department))
                .Select(c => c.Department)
                .Distinct()
                .ToListAsync();

            // Combine and remove duplicates
            return userDepartments.Union(chatbotDepartments)
                .OrderBy(d => d)
                .ToList();
        }

        public async Task<bool> DepartmentExistsAsync(string departmentName)
        {
            if (string.IsNullOrEmpty(departmentName))
                return false;

            return await _context.Users.AnyAsync(u => u.Department == departmentName) ||
                   await _context.Chatbots.AnyAsync(c => c.Department == departmentName);
        }

        public async Task<bool> CreateDepartmentAsync(string name, string userId)
        {
            try
            {
                // Create a default chatbot for this department
                var chatbot = new Chatbot
                {
                    Name = $"{name} Bot",
                    Department = name,
                    AiModel = "gpt-3.5-turbo",
                    Description = $"Default chatbot for {name} department",
                    CreatedBy = userId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Chatbots.Add(chatbot);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new department {Department} with default chatbot", name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department {Department}", name);
                return false;
            }
        }

        public async Task<bool> UpdateDepartmentAsync(string oldName, string newName)
        {
            try
            {
                // Begin transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Update users with the old department name
                var usersToUpdate = await _context.Users
                    .Where(u => u.Department == oldName)
                    .ToListAsync();

                foreach (var user in usersToUpdate)
                {
                    user.Department = newName;
                    _context.Update(user);
                }

                // Update chatbots with the old department name
                var chatbotsToUpdate = await _context.Chatbots
                    .Where(c => c.Department == oldName)
                    .ToListAsync();

                foreach (var chatbot in chatbotsToUpdate)
                {
                    chatbot.Department = newName;
                    _context.Update(chatbot);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated department name from {OldName} to {NewName}", oldName, newName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department from {OldName} to {NewName}", oldName, newName);
                return false;
            }
        }

        public async Task<bool> DeleteDepartmentAsync(string name)
        {
            try
            {
                // Delete chatbots in this department
                var chatbotsToDelete = await _context.Chatbots
                    .Where(c => c.Department == name)
                    .ToListAsync();

                if (chatbotsToDelete.Any())
                {
                    _context.Chatbots.RemoveRange(chatbotsToDelete);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Deleted department {Department} and its chatbots", name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department {Department}", name);
                return false;
            }
        }
    }
}