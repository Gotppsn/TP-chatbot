using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AIHelpdeskSupport.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AIHelpdeskSupport.Controllers.Api
{
    [Route("api/users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersApiController> _logger;

        public UsersApiController(
            UserManager<ApplicationUser> userManager,
            ILogger<UsersApiController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Gets all users with optional search filtering
        /// </summary>
        /// <param name="search">Optional search term to filter users</param>
        /// <param name="limit">Maximum number of results to return (default: 50)</param>
        /// <returns>List of users matching the criteria</returns>
        [HttpGet("getall")]
        public async Task<IActionResult> GetAllUsers(string search = "", int limit = 50)
        {
            try
            {
                _logger.LogInformation("Getting users with search: {Search}", 
                    string.IsNullOrEmpty(search) ? "[none]" : search);

                var query = _userManager.Users.AsQueryable();
                
                // Apply search filter if provided
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(u => 
                        u.UserName.ToLower().Contains(search) || 
                        u.Email.ToLower().Contains(search) || 
                        (u.FirstName != null && u.FirstName.ToLower().Contains(search)) || 
                        (u.LastName != null && u.LastName.ToLower().Contains(search)) ||
                        (u.Department != null && u.Department.ToLower().Contains(search))
                    );
                }
                
                // Apply limit to avoid too many results
                var users = await query
                    .Take(limit)
                    .Select(u => new {
                        userId = u.Id,
                        userName = u.UserName,
                        firstName = u.FirstName ?? "",
                        lastName = u.LastName ?? "",
                        email = u.Email,
                        department = u.Department
                    })
                    .ToListAsync();
                
                _logger.LogInformation("Retrieved {Count} users", users.Count);
                    
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { error = "An error occurred while retrieving users", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User information if found</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }
                
                return Ok(new {
                    userId = user.Id,
                    userName = user.UserName,
                    firstName = user.FirstName ?? "",
                    lastName = user.LastName ?? "",
                    email = user.Email,
                    department = user.Department,
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the user" });
            }
        }

        /// <summary>
        /// Gets users by department
        /// </summary>
        /// <param name="department">Department name</param>
        /// <returns>List of users in the specified department</returns>
        [HttpGet("bydepartment/{department}")]
        public async Task<IActionResult> GetUsersByDepartment(string department)
        {
            try
            {
                if (string.IsNullOrEmpty(department))
                {
                    return BadRequest(new { error = "Department name is required" });
                }
                
                var users = await _userManager.Users
                    .Where(u => u.Department == department)
                    .Select(u => new {
                        userId = u.Id,
                        userName = u.UserName,
                        firstName = u.FirstName ?? "",
                        lastName = u.LastName ?? "",
                        email = u.Email
                    })
                    .ToListAsync();
                
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for department {Department}", department);
                return StatusCode(500, new { error = "An error occurred while retrieving department users" });
            }
        }

        /// <summary>
        /// Checks if a username exists
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <returns>True if the username exists</returns>
        [HttpGet("exists/{username}")]
        public async Task<IActionResult> CheckUsernameExists(string username)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(username);
                return Ok(new { exists = user != null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if username exists: {Username}", username);
                return StatusCode(500, new { error = "An error occurred while checking username" });
            }
        }
    }
}