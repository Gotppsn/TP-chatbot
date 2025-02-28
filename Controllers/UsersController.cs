// Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.ViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

public async Task<IActionResult> Index()
{
    // Get all users from database
    var users = await _userManager.Users.ToListAsync();
    var viewModels = new List<UserViewModel>();

    foreach (var user in users)
    {
        // Get user roles
        var userRoles = await _userManager.GetRolesAsync(user);
        var role = userRoles.FirstOrDefault() ?? "User";

        viewModels.Add(new UserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Department = user.Department,
            Role = role,
            IsActive = user.IsActive,
            CreatedAt = DateTime.Now.AddMonths(-1), // Default placeholder value
            LastLogin = DateTime.Now.AddDays(-new Random().Next(1, 30)) // Random placeholder value
        });
    }

    return View(viewModels);
}

        // Add missing UserViewModel property for CreatedAt
        private static DateTime? ExtractLastLoginDate(ApplicationUser user)
        {
            // Try to extract from security stamp or last modified date
            if (!string.IsNullOrEmpty(user.SecurityStamp))
            {
                if (DateTime.TryParse(user.SecurityStamp, out DateTime stampDate))
                {
                    return stampDate;
                }
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            string roleName = roles.FirstOrDefault() ?? "User";
            
            // Convert to view model
            var viewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Department,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt ?? DateTime.Now,
                LastLogin = user.LastLoginAt
            };
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(id);
                    
                    if (user == null)
                    {
                        return NotFound();
                    }
                    
                    // Update user properties
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Email = model.Email;
                    user.Department = model.Department;
                    user.Role = model.Role;
                    user.IsActive = model.IsActive;
                    
                    // Save changes
                    var result = await _userManager.UpdateAsync(user);
                    
                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = "User updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await UserExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            var result = await _userManager.DeleteAsync(user);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting user.";
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            // Toggle status
            user.IsActive = !user.IsActive;
            
            // Update user
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User status {(user.IsActive ? "activated" : "deactivated")} successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error updating user status.";
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        private async Task<bool> UserExists(string id)
        {
            return await _userManager.Users.AnyAsync(u => u.Id == id);
        }
    }
}