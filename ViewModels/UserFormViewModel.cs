// ViewModels/UserFormViewModel.cs
public class UserFormViewModel
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
    
    [DataType(DataType.Password)]
    public string Password { get; set; }
    
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Password and confirmation do not match.")]
    public string ConfirmPassword { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    
    public SelectList Roles { get; set; }
    public SelectList Departments { get; set; }
    public List<PermissionItem> Permissions { get; set; } = new List<PermissionItem>();
}

public class PermissionItem
{
    public string Name { get; set; }
    public bool IsGranted { get; set; }
}