// Services/IPermissionService.cs
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string userId, string permission);
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);
    Task GrantPermissionAsync(string userId, string permission);
    Task RevokePermissionAsync(string userId, string permission);
    Task SetPermissionsAsync(string userId, IEnumerable<string> permissions);
}

