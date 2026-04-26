using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MH.Capstone.WebApp.Filters
{
    /// <summary>
    /// Redirects authenticated users whose DisplayName is still "UNSET" to the
    /// SetDisplayName page before they can reach any other part of the site.
    /// </summary>
    public class RequireDisplayNameFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> _exemptActions = new(StringComparer.OrdinalIgnoreCase)
        {
            "SetDisplayName",
            "Login",
            "Logout",
            "Register",
            "RegisterConfirmation",
            "VerifyEmail",
            "ResendVerification",
            "ForgotPassword",
            "ResetPassword",
            "ResetPasswordInvalid",
            "Reactivate",
            "Deactivate",
            "GeneratePasswordResetLink",
            "GenerateEmailConfirmationLink",
        };

        private readonly UserManager<ApplicationUser> _userManager;

        public RequireDisplayNameFilter(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
            if (_exemptActions.Contains(action))
            {
                await next();
                return;
            }

            var appUser = await _userManager.GetUserAsync(user);
            if (appUser?.DisplayName == "UNSET")
            {
                context.Result = new RedirectToActionResult("SetDisplayName", "Account", null);
                return;
            }

            await next();
        }
    }
}
