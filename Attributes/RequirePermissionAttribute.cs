// Attributes/RequirePermissionAttribute.cs
using System;
using System.Threading.Tasks;
using AIHelpdeskSupport.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using AIHelpdeskSupport.Models;

namespace AIHelpdeskSupport.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        
        public RequirePermissionAttribute(string permission)
        {
            _permission = permission;
        }
        
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            
            var user = await userManager.GetUserAsync(context.HttpContext.User);
            if (user == null)
            {
                context.Result = new ChallengeResult();
                return;
            }
            
            if (!await permissionService.HasPermissionAsync(user.Id, _permission))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}