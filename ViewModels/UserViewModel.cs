// ViewModels/UserViewModel.cs
using System;

namespace AIHelpdeskSupport.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}