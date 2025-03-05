using AIHelpdeskSupport.Data;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add ConnectionStringProvider service
builder.Services.AddSingleton<ConnectionStringProvider>();

// Add database with dynamic connection string
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var connectionProvider = serviceProvider.GetRequiredService<ConnectionStringProvider>();
    options.UseSqlServer(connectionProvider.GetConnectionString());
});

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Password settings - Simplified for development
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // Sign in settings
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

// LDAP services
builder.Services.AddScoped<ILdapAuthenticationService, LdapAuthenticationService>();
builder.Services.AddScoped<ILdapUserDataParser, LdapUserDataParser>();

// Add Flowise service
builder.Services.AddHttpClient();
builder.Services.AddScoped<IFlowiseService, FlowiseService>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Initialize connection string from database settings if available
using (var scope = app.Services.CreateScope())
{
    try
    {
        // Use appsettings.json connection string for initial database access
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database exists
        context.Database.EnsureCreated();
        
        // Load settings and update connection string provider
        var settings = context.SystemSettings.FirstOrDefault();
        if (settings != null)
        {
            ConnectionStringProvider.UpdateConnectionString(settings);
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error initializing connection string from settings");
    }
}

// Create scope for database initialization and user seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try 
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated(); // Just create database without migrations for testing
        
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Seed users and roles
        await IdentityDataInitializer.SeedRolesAsync(roleManager);
        await IdentityDataInitializer.SeedUsersAsync(userManager);
        await IdentityDataInitializer.UpdateUserDepartmentClaims(userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "chatSession",
        pattern: "UserChat/Chat/{sessionId}",
        defaults: new { controller = "UserChat", action = "ChatSession" });
        
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// User Seeding Class
public static class IdentityDataInitializer
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        // Create roles if they don't exist
        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
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
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else
            {
                var errorString = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Error creating admin user: {errorString}");
            }
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
            {
                await userManager.AddToRoleAsync(regularUser, "User");
            }
            else
            {
                var errorString = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Error creating regular user: {errorString}");
            }
        }

        // Create IT support user
        var itUser = await userManager.FindByNameAsync("it");
        if (itUser == null)
        {
            itUser = new ApplicationUser
            {
                UserName = "it",
                Email = "it@example.com",
                FirstName = "IT",
                LastName = "Support",
                Department = "IT Support",
                Role = "User",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(itUser, "it");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(itUser, "User");
            }
        }

        // Create sales user
        var salesUser = await userManager.FindByNameAsync("sales");
        if (salesUser == null)
        {
            salesUser = new ApplicationUser
            {
                UserName = "sales",
                Email = "sales@example.com",
                FirstName = "Sales",
                LastName = "Representative",
                Department = "Sales",
                Role = "User",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(salesUser, "sales");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser, "User");
            }
        }

        // Create billing user
        var billingUser = await userManager.FindByNameAsync("billing");
        if (billingUser == null)
        {
            billingUser = new ApplicationUser
            {
                UserName = "billing",
                Email = "billing@example.com",
                FirstName = "Billing",
                LastName = "Specialist",
                Department = "Billing",
                Role = "User",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(billingUser, "billing");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(billingUser, "User");
            }
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
                
            // Remove existing department claim if exists
            if (departmentClaim != null)
            {
                await userManager.RemoveClaimAsync(user, departmentClaim);
            }
            
            // Remove existing role claim if exists
            if (roleClaim != null)
            {
                await userManager.RemoveClaimAsync(user, roleClaim);
            }
                
            // Add department claim
            if (!string.IsNullOrEmpty(user.Department))
            {
                await userManager.AddClaimAsync(user, new Claim("Department", user.Department));
            }
            
            // Add role claim
            if (!string.IsNullOrEmpty(user.Role))
            {
                await userManager.AddClaimAsync(user, new Claim("Role", user.Role));
            }
        }
    }
}