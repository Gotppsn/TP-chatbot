// Services/PermissionService.cs
public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public PermissionService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        
        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return true;
            
        return await _context.UserPermissions
            .AnyAsync(up => up.UserId == userId && up.PermissionName == permission && up.IsGranted);
    }
    
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
    {
        return await _context.UserPermissions
            .Where(up => up.UserId == userId && up.IsGranted)
            .Select(up => up.PermissionName)
            .ToListAsync();
    }
    
    public async Task GrantPermissionAsync(string userId, string permission)
    {
        var existingPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionName == permission);
            
        if (existingPermission != null)
        {
            existingPermission.IsGranted = true;
        }
        else
        {
            await _context.UserPermissions.AddAsync(new UserPermission
            {
                UserId = userId,
                PermissionName = permission,
                IsGranted = true
            });
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task RevokePermissionAsync(string userId, string permission)
    {
        var existingPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionName == permission);
            
        if (existingPermission != null)
        {
            existingPermission.IsGranted = false;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task SetPermissionsAsync(string userId, IEnumerable<string> permissions)
    {
        var existingPermissions = await _context.UserPermissions
            .Where(up => up.UserId == userId)
            .ToListAsync();
            
        _context.UserPermissions.RemoveRange(existingPermissions);
        
        foreach (var permission in permissions)
        {
            await _context.UserPermissions.AddAsync(new UserPermission
            {
                UserId = userId,
                PermissionName = permission,
                IsGranted = true
            });
        }
        
        await _context.SaveChangesAsync();
    }
}