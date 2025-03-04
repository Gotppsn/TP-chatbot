// Models/UserPermission.cs
using Microsoft.AspNetCore.Identity;

namespace AIHelpdeskSupport.Models
{
    public class UserPermission
    {
        public string UserId { get; set; }
        public string PermissionName { get; set; }
        public bool IsGranted { get; set; } = true;
        
        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
}