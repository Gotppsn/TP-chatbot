using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AIHelpdeskSupport.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string FirstName { get; set; }
        
        [Required]
        public string LastName { get; set; }
        
        [Required]
        public string Department { get; set; }
        
        [Required]
        public string Role { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? LastLogin { get; set; }
        
        // Password reset fields
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
        
        // For the password reset option
        public string ResetPasswordOption { get; set; } = "no";
        
        // For permissions
        public List<PermissionViewModel> Permissions { get; set; } = new List<PermissionViewModel>();
        
        // Computed property for full name
        public string FullName => $"{FirstName} {LastName}";
    }
    
    public class PermissionViewModel
    {
        public string Name { get; set; }
        public bool IsGranted { get; set; }
    }
}