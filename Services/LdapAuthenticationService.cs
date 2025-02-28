// Services/LdapAuthenticationService.cs
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
        private readonly string _domain;
        private readonly ILogger<LdapAuthenticationService> _logger;

        public LdapAuthenticationService(IConfiguration configuration, ILogger<LdapAuthenticationService> logger)
        {
            _domain = configuration["LDAP:Domain"] ?? "thaiparkerizing";
            _logger = logger;
        }

        public async Task<ApplicationUser?> AuthenticateAsync(string username, string password)
        {
            try
            {
                bool isValid = ValidateCredentials(username, password, _domain);
                
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
                using (var context = new PrincipalContext(ContextType.Domain, _domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    
                    if (user == null)
                    {
                        return null;
                    }

                    // Create ApplicationUser from LDAP user
                    var appUser = new ApplicationUser
                    {
                        UserName = username,
                        Email = user.EmailAddress,
                        FirstName = user.GivenName ?? "",
                        LastName = user.Surname ?? "",
                        Department = GetUserDepartment(user)
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
                // Try to get department from directory entry
                var directoryEntry = (user.GetUnderlyingObject() as System.DirectoryServices.DirectoryEntry);
                if (directoryEntry?.Properties["department"]?.Value != null)
                {
                    return directoryEntry.Properties["department"].Value.ToString() ?? "";
                }
                
                // Default department if not found
                return "Customer Service";
            }
            catch
            {
                return "Customer Service";
            }
        }

        private bool ValidateCredentials(string username, string password, string domain)
        {
            using (var context = new PrincipalContext(ContextType.Domain, domain))
            {
                return context.ValidateCredentials(username, password);
            }
        }
    }
}