using System.DirectoryServices.AccountManagement;
using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.Services
{
    public interface ILdapAuthenticationService
    {
        Task<ApplicationUser?> AuthenticateAsync(string username, string password);
        Task<ApplicationUser?> GetUserDetailsAsync(string username);
    }

    public class LdapAuthenticationService : ILdapAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LdapAuthenticationService> _logger;

        public LdapAuthenticationService(IConfiguration configuration, ILogger<LdapAuthenticationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ApplicationUser?> AuthenticateAsync(string username, string password)
        {
            try
            {
                string domain = _configuration["LDAP:Domain"] ?? "thaiparkerizing";
                bool isValid = ValidateCredentials(username, password, domain);
                
                if (!isValid)
                {
                    return null;
                }

                // Get user details from LDAP
                return await GetUserDetailsAsync(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LDAP authentication failed for user {Username}", username);
                return null;
            }
        }

        public async Task<ApplicationUser?> GetUserDetailsAsync(string username)
        {
            try
            {
                string domain = _configuration["LDAP:Domain"] ?? "thaiparkerizing";
                using (var context = new PrincipalContext(ContextType.Domain, domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    
                    if (user == null)
                    {
                        return null;
                    }

                    // Try to get department from directory entry
                    string department = GetUserDepartment(user);
                    
                    // Map LDAP user to application user
                    var appUser = new ApplicationUser
                    {
                        UserName = username,
                        Email = user.EmailAddress ?? $"{username}@{domain}.com",
                        FirstName = user.GivenName ?? username,
                        LastName = user.Surname ?? "",
                        Department = department,
                        Role = "User",
                        IsActive = true
                    };

                    return appUser;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get LDAP user details for {Username}", username);
                return null;
            }
        }

        private string GetUserDepartment(UserPrincipal user)
        {
            try
            {
                // Default department from config
                string defaultDepartment = _configuration["LDAP:DefaultDepartment"] ?? "Customer Service";
                
                // Try to get department from directory entry
                var directoryEntry = (user.GetUnderlyingObject() as System.DirectoryServices.DirectoryEntry);
                if (directoryEntry?.Properties["department"]?.Value != null)
                {
                    return directoryEntry.Properties["department"].Value.ToString() ?? defaultDepartment;
                }
                
                // Fallback to default department
                return defaultDepartment;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving department for user {Username}", user.SamAccountName);
                return "Customer Service"; // Default on error
            }
        }

        private bool ValidateCredentials(string username, string password, string domain)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, domain))
                {
                    return context.ValidateCredentials(username, password);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for {Username}", username);
                return false;
            }
        }
    }
}