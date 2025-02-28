// Services/LdapUserDataParser.cs
using System.Text.Json;
using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.Services
{
    public interface ILdapUserDataParser
    {
        ApplicationUser ParseUserData(string jsonData, string username);
    }

    public class LdapUserDataParser : ILdapUserDataParser
    {
        private readonly ILogger<LdapUserDataParser> _logger;

        public LdapUserDataParser(ILogger<LdapUserDataParser> logger)
        {
            _logger = logger;
        }

        public ApplicationUser ParseUserData(string jsonData, string username)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonData))
                {
                    var root = doc.RootElement;
                    
                    // Extract user info
                    var user = new ApplicationUser
                    {
                        UserName = username,
                        Email = root.GetProperty("Users").GetProperty("User_Email").GetString() ?? "",
                        FirstName = root.GetProperty("Detail_EN_FirstName").GetString() ?? "",
                        LastName = root.GetProperty("Detail_EN_LastName").GetString() ?? ""
                    };
                    
                    // Try to get department information
                    try
                    {
                        var department = root.GetProperty("Users")
                            .GetProperty("Master_Processes")
                            .GetProperty("Master_Sections")
                            .GetProperty("Master_Departments")
                            .GetProperty("Department_Name")
                            .GetString();
                            
                        user.Department = department ?? "Information Technology";
                    }
                    catch
                    {
                        user.Department = "Information Technology"; // Default if not found
                    }
                    
                    return user;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing LDAP user data JSON");
                
                // Return basic user if parsing fails
                return new ApplicationUser
                {
                    UserName = username,
                    Email = $"{username}@thaiparker.co.th",
                    FirstName = username,
                    LastName = "",
                    Department = "Information Technology"
                };
            }
        }
    }
}