// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace AIHelpdeskSupport.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}