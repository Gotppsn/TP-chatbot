// Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.ViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIHelpdeskSupport.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // In a real application, we would fetch from the database
            // and paginate the results for better performance

            // For now, we'll use sample data to demonstrate the UI
            var users = GetSampleUsers();

            return View(users);
        }

        private List<UserViewModel> GetSampleUsers()
        {
            // Create sample data for demonstration
            return new List<UserViewModel>
            {
                new UserViewModel {
                    Id = "1",
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    Department = "Administration",
                    Role = "Administrator",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-6),
                    LastLogin = DateTime.Now.AddDays(-1)
                },
                new UserViewModel {
                    Id = "2",
                    Email = "john.doe@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    Department = "Customer Service",
                    Role = "Department Manager",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-5),
                    LastLogin = DateTime.Now.AddDays(-2)
                },
                new UserViewModel {
                    Id = "3",
                    Email = "jane.smith@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Department = "IT Support",
                    Role = "Department Manager",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-4),
                    LastLogin = DateTime.Now.AddHours(-12)
                },
                new UserViewModel {
                    Id = "4",
                    Email = "robert.johnson@example.com",
                    FirstName = "Robert",
                    LastName = "Johnson",
                    Department = "Sales",
                    Role = "Support Agent",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    LastLogin = DateTime.Now.AddDays(-5)
                },
                new UserViewModel {
                    Id = "5",
                    Email = "sarah.williams@example.com",
                    FirstName = "Sarah",
                    LastName = "Williams",
                    Department = "Billing",
                    Role = "Support Agent",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-2),
                    LastLogin = DateTime.Now.AddMinutes(-30)
                },
                new UserViewModel {
                    Id = "6",
                    Email = "michael.brown@example.com",
                    FirstName = "Michael",
                    LastName = "Brown",
                    Department = "Technical",
                    Role = "Support Agent",
                    IsActive = false,
                    CreatedAt = DateTime.Now.AddMonths(-8),
                    LastLogin = DateTime.Now.AddMonths(-1)
                },
                new UserViewModel {
                    Id = "7",
                    Email = "emily.davis@example.com",
                    FirstName = "Emily",
                    LastName = "Davis",
                    Department = "Customer Service",
                    Role = "Support Agent",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-20),
                    LastLogin = DateTime.Now.AddHours(-2)
                },
                new UserViewModel {
                    Id = "8",
                    Email = "david.miller@example.com",
                    FirstName = "David",
                    LastName = "Miller",
                    Department = "Operations",
                    Role = "Department Manager",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-25),
                    LastLogin = DateTime.Now.AddDays(-3)
                }
            };
        }
        [HttpGet]
        public IActionResult Edit(string id)
        {
            // In a real application, we would fetch the user from the database
            // For now, we'll use sample data
            var users = GetSampleUsers();
            var user = users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
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
                // In a real application, we would update the user in the database
                // For now, we'll just show success message and redirect
                TempData["SuccessMessage"] = "User updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            // In a real application, we would delete the user from the database
            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}