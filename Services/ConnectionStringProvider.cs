using AIHelpdeskSupport.Models;
using Microsoft.Data.SqlClient;

namespace AIHelpdeskSupport.Services
{
    public class ConnectionStringProvider
    {
        private readonly IConfiguration _configuration;
        private static string _cachedConnectionString;
        private static readonly string _fallbackConnectionString = "Server=172.101.1.19;Database=AIHelpdeskSupport;User Id=sa;Password=Parker789;TrustServerCertificate=True;MultipleActiveResultSets=true;Integrated Security=False;";

        public ConnectionStringProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetConnectionString()
        {
            try
            {
                if (!string.IsNullOrEmpty(_cachedConnectionString))
                    return _cachedConnectionString;

                var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
                return !string.IsNullOrEmpty(defaultConnection) ? defaultConnection : _fallbackConnectionString;
            }
            catch
            {
                return _fallbackConnectionString;
            }
        }

        public static void UpdateConnectionString(SystemSettings settings)
        {
            if (settings == null)
                return;

            try
            {
                var newConnectionString = BuildConnectionString(settings);
                
                // Test connection before updating cached string
                using (var connection = new SqlConnection(newConnectionString))
                {
                    connection.Open();
                    connection.Close();
                }
                
                _cachedConnectionString = newConnectionString;
            }
            catch
            {
                // Keep existing connection string if the new one fails
            }
        }

        public static string BuildConnectionString(SystemSettings settings)
        {
            return $"Server={settings.SqlServerHost};" +
                   $"Database={settings.SqlServerDatabase};" +
                   $"User Id={settings.SqlServerUsername};" +
                   $"Password={settings.SqlServerPassword};" +
                   $"TrustServerCertificate={settings.SqlServerTrustServerCertificate};" +
                   $"MultipleActiveResultSets={settings.SqlServerMultipleActiveResultSets};" +
                   $"Integrated Security=False;";
        }
    }
}