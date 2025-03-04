using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.ViewModels;
using AIHelpdeskSupport.Services;
using System.Threading.Tasks;
using System.Security.Claims;

namespace AIHelpdeskSupport.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILdapAuthenticationService _ldapService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger,
            IConfiguration configuration,
            ILdapAuthenticationService ldapService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
            _ldapService = ldapService;
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
                // First check if LDAP is enabled in configuration
                bool ldapEnabled = _configuration.GetValue<bool>("LDAP:Enabled", false);

                if (ldapEnabled)
                {
                    // Try LDAP authentication first
                    var ldapUser = await _ldapService.AuthenticateAsync(model.Email, model.Password);

                    if (ldapUser != null)
                    {
                        // User authenticated via LDAP
                        _logger.LogInformation("User authenticated via LDAP: {Email}", model.Email);

                        // Check if the user exists in our database
                        var existingUser = await _userManager.FindByNameAsync(ldapUser.UserName);

                        if (existingUser == null)
                        {
                            // Create new user with LDAP info
                            ldapUser.EmailConfirmed = true;
                            ldapUser.IsActive = true;

                            var createResult = await _userManager.CreateAsync(ldapUser);
                            if (createResult.Succeeded)
                            {
                                // Assign default User role
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
                            // Update existing user info from LDAP
                            existingUser.Email = ldapUser.Email;
                            existingUser.FirstName = ldapUser.FirstName;
                            existingUser.LastName = ldapUser.LastName;
                            existingUser.Department = ldapUser.Department;

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

                        // Sign in the user
                        await _signInManager.SignInAsync(existingUser, model.RememberMe);

                        // Determine redirect based on user role
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

                // Fall back to regular Identity login
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

                        user.LastLoginAt = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);

                        var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                        await dbContext.SaveChangesAsync();

                        // Sign in user
                        await _signInManager.SignInAsync(user, model.RememberMe);

                        // Determine redirect based on user role
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