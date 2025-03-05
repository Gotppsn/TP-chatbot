// Add new file: Services/ConnectionStringProvider.cs
using AIHelpdeskSupport.Data;
using AIHelpdeskSupport.Models;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Services
{
    public class ConnectionStringProvider
    {
        private readonly IConfiguration _configuration;
        private static string _cachedConnectionString;

        public ConnectionStringProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetConnectionString()
        {
            // Return cached connection string if available
            if (!string.IsNullOrEmpty(_cachedConnectionString))
                return _cachedConnectionString;

            // Fallback to appsettings.json
            return _configuration.GetConnectionString("DefaultConnection");
        }

        public static void UpdateConnectionString(SystemSettings settings)
        {
            if (settings == null)
                return;

            _cachedConnectionString = BuildConnectionString(settings);
        }

        public static string BuildConnectionString(SystemSettings settings)
        {
            return $"Server={settings.SqlServerHost};" +
                   $"Database={settings.SqlServerDatabase};" +
                   $"User Id={settings.SqlServerUsername};" +
                   $"Password={settings.SqlServerPassword};" +
                   $"TrustServerCertificate={settings.SqlServerTrustServerCertificate};" +
                   $"MultipleActiveResultSets={settings.SqlServerMultipleActiveResultSets}";
        }
    }
}