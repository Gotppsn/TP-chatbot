using AIHelpdeskSupport.Data;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .EnableSensitiveDataLogging()
    .LogTo(
        message => Console.WriteLine($"SQL: {message}"), 
        new[] { DbLoggerCategory.Database.Command.Name },
        LogLevel.Information));

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
});

// Add services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IFlowiseService, FlowiseService>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ILdapAuthenticationService, LdapAuthenticationService>();
builder.Services.AddScoped<ILdapUserDataParser, LdapUserDataParser>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Initialize database and seed users
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try 
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        await IdentityDataInitializer.SeedRolesAsync(roleManager);
        await IdentityDataInitializer.SeedUsersAsync(userManager);
        await IdentityDataInitializer.UpdateUserDepartmentClaims(userManager);
        
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the database");
    }
}

// Configure HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Configure routes
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "chatSession",
        pattern: "UserChat/ChatSession/{sessionId}",
        defaults: new { controller = "UserChat", action = "ChatSession" });
        
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.Run();

// User Seeding Class
public static class IdentityDataInitializer
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    public static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        // Create admin user
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                Department = "Administration",
                Role = "Administrator",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "admin");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, "Admin");
            else
                throw new Exception($"Error creating admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Create regular user
        var regularUser = await userManager.FindByNameAsync("user");
        if (regularUser == null)
        {
            regularUser = new ApplicationUser
            {
                UserName = "user",
                Email = "user@example.com",
                FirstName = "Regular",
                LastName = "User",
                Department = "Customer Service",
                Role = "User",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(regularUser, "user");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(regularUser, "User");
            else
                throw new Exception($"Error creating regular user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    public static async Task UpdateUserDepartmentClaims(UserManager<ApplicationUser> userManager)
    {
        var users = userManager.Users.ToList();
        foreach (var user in users)
        {
            var currentClaims = await userManager.GetClaimsAsync(user);
            var departmentClaim = currentClaims.FirstOrDefault(c => c.Type == "Department");
            var roleClaim = currentClaims.FirstOrDefault(c => c.Type == "Role");
                
            if (departmentClaim != null)
                await userManager.RemoveClaimAsync(user, departmentClaim);
            
            if (roleClaim != null)
                await userManager.RemoveClaimAsync(user, roleClaim);
                
            if (!string.IsNullOrEmpty(user.Department))
                await userManager.AddClaimAsync(user, new Claim("Department", user.Department));
            
            if (!string.IsNullOrEmpty(user.Role))
                await userManager.AddClaimAsync(user, new Claim("Role", user.Role));
        }
    }
}