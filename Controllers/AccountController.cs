using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.ViewModels;
using AIHelpdeskSupport.Services;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System;
using AIHelpdeskSupport.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AIHelpdeskSupport.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILdapAuthenticationService _ldapService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger,
            IConfiguration configuration,
            ILdapAuthenticationService ldapService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
            _ldapService = ldapService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                bool ldapEnabled = _configuration.GetValue<bool>("LDAP:Enabled", false);

                if (ldapEnabled)
                {
                    var ldapUser = await _ldapService.AuthenticateAsync(model.Email, model.Password);

                    if (ldapUser != null)
                    {
                        _logger.LogInformation("User authenticated via LDAP: {Email}", model.Email);
                        var existingUser = await _userManager.FindByNameAsync(ldapUser.UserName);

                        if (existingUser == null)
                        {
                            // Auto-create department if it doesn't exist
                            if (!string.IsNullOrEmpty(ldapUser.Department))
                            {
                                using (var scope = _serviceScopeFactory.CreateScope())
                                {
                                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                                    
                                    // Check if department exists by looking for any user or chatbot with this department
                                    bool departmentExists = await dbContext.Users.AnyAsync(u => u.Department == ldapUser.Department) ||
                                                         await dbContext.Chatbots.AnyAsync(c => c.Department == ldapUser.Department);
                                    
                                    if (!departmentExists)
                                    {
                                        // Create a default chatbot for this department
                                        var chatbot = new Chatbot
                                        {
                                            Name = $"{ldapUser.Department} Bot",
                                            Department = ldapUser.Department,
                                            AiModel = "gpt-3.5-turbo",
                                            Description = $"Default chatbot for {ldapUser.Department} department",
                                            CreatedBy = "System",
                                            IsActive = true
                                        };
                                        
                                        dbContext.Chatbots.Add(chatbot);
                                        await dbContext.SaveChangesAsync();
                                        
                                        _logger.LogInformation("Created new department {Department} for user {Username}", 
                                            ldapUser.Department, ldapUser.UserName);
                                    }
                                }
                            }

                            ldapUser.EmailConfirmed = true;
                            ldapUser.IsActive = true;
                            ldapUser.LastLoginAt = DateTime.Now;

                            var createResult = await _userManager.CreateAsync(ldapUser);
                            if (createResult.Succeeded)
                            {
                                await _userManager.AddToRoleAsync(ldapUser, "User");
                                await _userManager.AddClaimAsync(ldapUser, new Claim("Department", ldapUser.Department));
                                existingUser = ldapUser;
                            }
                            else
                            {
                                foreach (var error in createResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                                return View(model);
                            }
                        }
                        else
                        {
                            // Update existing user with LDAP data
                            existingUser.Email = ldapUser.Email;
                            existingUser.FirstName = ldapUser.FirstName;
                            existingUser.LastName = ldapUser.LastName;
                            
                            // Update department and create if necessary
                            if (!string.IsNullOrEmpty(ldapUser.Department) && existingUser.Department != ldapUser.Department)
                            {
                                using (var scope = _serviceScopeFactory.CreateScope())
                                {
                                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                                    
                                    // Check if new department exists
                                    bool departmentExists = await dbContext.Users.AnyAsync(u => u.Department == ldapUser.Department) ||
                                                         await dbContext.Chatbots.AnyAsync(c => c.Department == ldapUser.Department);
                                    
                                    if (!departmentExists)
                                    {
                                        // Create a default chatbot for this department
                                        var chatbot = new Chatbot
                                        {
                                            Name = $"{ldapUser.Department} Bot",
                                            Department = ldapUser.Department,
                                            AiModel = "gpt-3.5-turbo",
                                            Description = $"Default chatbot for {ldapUser.Department} department",
                                            CreatedBy = "System",
                                            IsActive = true
                                        };
                                        
                                        dbContext.Chatbots.Add(chatbot);
                                        await dbContext.SaveChangesAsync();
                                        
                                        _logger.LogInformation("Created new department {Department} for user {Username}", 
                                            ldapUser.Department, ldapUser.UserName);
                                    }
                                }
                                
                                existingUser.Department = ldapUser.Department;
                            }
                            
                            existingUser.LastLoginAt = DateTime.Now;
                            await _userManager.UpdateAsync(existingUser);
                            
                            // Update department claim
                            var claims = await _userManager.GetClaimsAsync(existingUser);
                            var deptClaim = claims.FirstOrDefault(c => c.Type == "Department");
                            if (deptClaim != null)
                            {
                                await _userManager.RemoveClaimAsync(existingUser, deptClaim);
                            }
                            await _userManager.AddClaimAsync(existingUser, new Claim("Department", existingUser.Department));
                        }

                        await _signInManager.SignInAsync(existingUser, model.RememberMe);

                        var roles = await _userManager.GetRolesAsync(existingUser);
                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Index", "Dashboard");
                        }
                        else
                        {
                            return RedirectToAction("Index", "UserChat");
                        }
                    }
                }

                var userName = model.Email;
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    user = await _userManager.FindByNameAsync(model.Email);
                }

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        user.UserName,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in: {Email}", model.Email);

                        user.LastLoginAt = DateTime.Now;
                        await _userManager.UpdateAsync(user);

                        // Ensure department exists
                        if (!string.IsNullOrEmpty(user.Department))
                        {
                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                                
                                // Check if department exists
                                bool departmentExists = await dbContext.Chatbots.AnyAsync(c => c.Department == user.Department);
                                
                                if (!departmentExists)
                                {
                                    // Create a default chatbot for this department
                                    var chatbot = new Chatbot
                                    {
                                        Name = $"{user.Department} Bot",
                                        Department = user.Department,
                                        AiModel = "gpt-3.5-turbo",
                                        Description = $"Default chatbot for {user.Department} department",
                                        CreatedBy = "System",
                                        IsActive = true
                                    };
                                    
                                    dbContext.Chatbots.Add(chatbot);
                                    await dbContext.SaveChangesAsync();
                                    
                                    _logger.LogInformation("Created new department {Department} for local user {Username}", 
                                        user.Department, user.UserName);
                                }
                            }
                        }

                        await _signInManager.SignInAsync(user, model.RememberMe);

                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Index", "Dashboard");
                        }
                        else if (roles.Contains("User"))
                        {
                            return RedirectToAction("Index", "UserChat");
                        }

                        return RedirectToLocal(returnUrl);
                    }

                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out: {Email}", model.Email);
                        ModelState.AddModelError(string.Empty, "Account locked. Please try again later.");
                        return View(model);
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}