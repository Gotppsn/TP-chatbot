using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AIHelpdeskSupport.Services
{
    public class DatabaseMigrationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(IConfiguration configuration, ILogger<DatabaseMigrationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task MigrateAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    await AddSqlServerColumnsIfNotExistAsync(connection);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying database migration");
                }
            }
        }

        private async Task AddSqlServerColumnsIfNotExistAsync(SqlConnection connection)
        {
            string[] columns = new string[] 
            { 
                "SqlServerHost", "SqlServerDatabase", "SqlServerUsername", 
                "SqlServerPassword", "SqlServerTrustServerCertificate", "SqlServerMultipleActiveResultSets" 
            };

            foreach (var column in columns)
            {
                if (!await ColumnExistsAsync(connection, "SystemSettings", column))
                {
                    string sql = GetColumnCreationSql(column);
                    using (var command = new SqlCommand(sql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        _logger.LogInformation($"Added column {column} to SystemSettings table");
                    }
                }
            }
        }

        private async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName)
        {
            string sql = @"
                SELECT COUNT(1) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @tableName 
                AND COLUMN_NAME = @columnName";

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@tableName", SqlDbType.NVarChar).Value = tableName;
                command.Parameters.Add("@columnName", SqlDbType.NVarChar).Value = columnName;
                
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
        }

        private string GetColumnCreationSql(string columnName)
        {
            return columnName switch
            {
                "SqlServerHost" => "ALTER TABLE SystemSettings ADD SqlServerHost NVARCHAR(255) NOT NULL DEFAULT 'localhost'",
                "SqlServerDatabase" => "ALTER TABLE SystemSettings ADD SqlServerDatabase NVARCHAR(255) NOT NULL DEFAULT 'AIHelpdeskSupport'",
                "SqlServerUsername" => "ALTER TABLE SystemSettings ADD SqlServerUsername NVARCHAR(255) NOT NULL DEFAULT 'sa'",
                "SqlServerPassword" => "ALTER TABLE SystemSettings ADD SqlServerPassword NVARCHAR(255) NOT NULL DEFAULT ''",
                "SqlServerTrustServerCertificate" => "ALTER TABLE SystemSettings ADD SqlServerTrustServerCertificate BIT NOT NULL DEFAULT 1",
                "SqlServerMultipleActiveResultSets" => "ALTER TABLE SystemSettings ADD SqlServerMultipleActiveResultSets BIT NOT NULL DEFAULT 1",
                _ => throw new ArgumentException($"Unknown column name: {columnName}")
            };
        }
    }
}