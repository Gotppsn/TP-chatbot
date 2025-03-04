using System.Collections.Generic;
using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.ViewModels
{
    public class UserFormViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; } = true;
        
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string ResetPasswordOption { get; set; } = "no";
        
        public List<UserPermissionViewModel> Permissions { get; set; } = new List<UserPermissionViewModel>();
    }
    
    public class UserPermissionViewModel
    {
        public string Name { get; set; }
        public bool IsGranted { get; set; }
        public string Description { get; set; }
        public string Group { get; set; }
    }
}