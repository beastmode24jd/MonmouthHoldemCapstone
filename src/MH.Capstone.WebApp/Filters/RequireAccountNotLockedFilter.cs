using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MH.Capstone.WebApp.Filters
{
    /// <summary>
    /// CSP-179: Enforces the Admin soft-lock (ApplicationUser.AccountLocked) on every
    /// request from an authenticated user. When the flag is true, the user is signed
    /// out and redirected to the Login page with a TempData message so the lock
    /// takes effect on the next request after an Admin toggles it — without waiting
    /// for the auth cookie to expire.
    /// </summary>
    public class RequireAccountNotLockedFilter : IAsyncActionFilter
    {
        public const string LockedMessageTempDataKey = "AccountLockedMessage";

        public const string LockedUserDisplayMessage =
            "This account has been locked by an administrator. Contact support to have it unlocked.";

        private static readonly HashSet<string> _exemptActions = new(StringComparer.OrdinalIgnoreCase)
        {
            "Login",
            "Logout",
            "Register",
            "RegisterConfirmation",
            "VerifyEmail",
            "ResendVerification",
            "ForgotPassword",
            "ResetPassword",
            "ResetPasswordInvalid",
            "GeneratePasswordResetLink",
            "GenerateEmailConfirmationLink",
        };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITempDataDictionaryFactory _tempDataFactory;

        public RequireAccountNotLockedFilter(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITempDataDictionaryFactory tempDataFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tempDataFactory = tempDataFactory;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var principal = context.HttpContext.User;

            if (principal.Identity?.IsAuthenticated != true)
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

            var appUser = await _userManager.GetUserAsync(principal);
            if (appUser is { AccountLocked: true })
            {
                await _signInManager.SignOutAsync();

                var tempData = _tempDataFactory.GetTempData(context.HttpContext);
                tempData[LockedMessageTempDataKey] = LockedUserDisplayMessage;
                tempData.Save();

                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            await next();
        }
    }
}
