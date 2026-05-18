using System.Text;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NotificationType = MH.Capstone.Domain.DataModels.NotificationType;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize]
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly FeatureFlags _featureFlags;
        private readonly IFollowService _followService;
        private readonly IBlockService _blockService;

        // Logger for tracking authentication-related events
        private readonly ILogger<AccountController> _logger;

        // Constructor: injects authentication service and logger via dependency injection
        public AccountController(
            IAuthenticationService authService,
            IUserService userService,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            FeatureFlags featureFlags,
            IFollowService followService,
            IBlockService blockService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _userService = userService;
            _userManager = userManager;
            _notificationService = notificationService;
            _featureFlags = featureFlags;
            _followService = followService;
            _blockService = blockService;
            _logger = logger;
        }

        [HttpGet]
        [Route("")]
        [Route("{id:guid}")]
        public async Task<IActionResult> Index(Guid? id)
        {
            var user = await _userManager.GetUserAsync(User);
            AccountViewModel vm;

            if (user == null)
            {
                return Unauthorized("You must be logged in and have a valid User Identity Claim to access this endpoint");
            }

            // Check if the optional id parameter is provided and valid,
            if (!id.HasValue || id == Guid.Empty)
            {
                // If not, use the current authenticated user
                // This is hit when the route("") endpoint is used, which allows for the "/account" endpoint
                vm = new AccountViewModel(user, true);
                _logger.LogInformation("No Id provided");
            }
            else
            {
                // If an ID is provided, this that id for viewing the account instead of the current user
                var userFromId = await _userManager.FindByIdAsync(id.Value.ToString());
                if (userFromId == null)
                {
                    return NotFound("No user found with the provided ID");
                }

                // Create an Account ViewModel for the user being viewed, and indicate whether they are the authenticated user
                vm = new AccountViewModel(userFromId, userFromId.Id == user.Id);
                _logger.LogInformation("Id provided");

                // CSP-187: only populate follow/block state when viewing another user.
                if (!vm.IsAuthenticatedUser)
                {
                    vm.IsFollowedByCurrentUser = await _followService.IsFollowingAsync(user.GuidId, vm.Id);
                    vm.IsBlockedByCurrentUser = await _blockService.IsBlockedAsync(user.GuidId, vm.Id);
                }
            }

            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            // Prevent logged-in users from accessing the login page
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // Store return URL so it can be used after login
            ViewData["ReturnUrl"] = returnUrl;

            // Render the login view
            return View();
        }

        // Displays the login page.
        // If the user is already authenticated, they are redirected to the dashboard.     
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Route("login")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            // Preserve return URL across postback
            ViewData["ReturnUrl"] = returnUrl;

            // Stop processing if validation attributes fail
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // First, check if user exists
            var user = await _userService.GetUserByEmailAsync(model.Email);
            
            // If user doesn't exist, show generic error
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // Verify password using SignInManager (works even for deactivated accounts)
            var passwordValid = await _authService.CheckPasswordAsync(model.Email, model.Password);

            // If password is wrong, show generic error (don't reveal account status)
            if (!passwordValid)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // Password is correct - block unverified accounts before revealing deactivation status
            if (!user.EmailConfirmed)
            {
                ViewData["ShowResendVerificationOption"] = true;
                TempData["UnverifiedEmail"] = model.Email;
                return View(model);
            }

            // Password is correct - now we can reveal if account is deactivated
            if (user.IsDeactivated)
            {
                TempData["DeactivatedEmail"] = model.Email;
                ModelState.AddModelError(string.Empty, "This account has been deactivated.");
                ViewData["ShowReactivateOption"] = true;
                return View(model);
            }

            // Get the login timestamp, since credentials are valid and the account is active
            var now = DateTimeOffset.UtcNow;

            if (user.LastLogin.HasValue)
            {
                // Get the difference between current time and LastLogin for
                //  the Application User
                var daysSinceLastLogin = (now - user.LastLogin.Value).TotalDays;

                if (daysSinceLastLogin <= 30)
                {
                    // Increment if 1 day has passed, to prevent page refreshes from potentially
                    // artificially inflating the streak
                    if (daysSinceLastLogin >= 1) 
                    {
                        user.LoginStreak++;
                    }
                }
                else
                {
                    user.LoginStreak = 1;
                }
            }
            else
            {
                user.LoginStreak = 1;
            }

            // Save the new LastLogin status to the ApplicationUser.
            user.LastLogin = now;
            await _userManager.UpdateAsync(user);

            // Now sign in!
            await _authService.SignInUserAsync(
                HttpContext,
                model.Email,
                model.RememberMe);

            _logger.LogInformation(
                "User {Email} logged in successfully",
                model.Email);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // Displays the registration page.
        // Redirects authenticated users to the dashboard.
        [HttpGet]
        [AllowAnonymous]
        [Route("Register")]
        public IActionResult Register()
        {
            // Prevent logged-in users from registering again
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // Render registration view
            return View();
        }


        // Processes registration form submission.
        // Creates a new user account and signs them in upon success.
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Route("Register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Stop processing if validation attributes fail
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Prevent duplicate user accounts
            if (await _userService.UserExistsAsync(model.Email))
            {
                // Log specific reason server-side to avoid email enumeration in responses
                _logger.LogWarning(
                    "Registration attempt with already registered email: {Email}",
                    model.Email);

                // Get the URL for the Login page redirect
                var loginUrl = Url.Action("Login", "Account");

                // Provide the direct message with the Login page link
                ModelState.AddModelError(
                    string.Empty,
                    $"Registration failed. If you think you have an account, try the <a href='{loginUrl}' class='alert-link'>Login page</a>.");

                return View(model);
            }

            // Attempt to register the user
            var success = await _authService.RegisterUserAsync(
                model.Email,
                model.Password,
                model.DisplayName);

            if (success)
            {
                _logger.LogInformation("New user registered: {Email}", model.Email);

                // Send email verification link — user must verify before logging in
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var rawToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
                    var confirmLink = Url.Action(
                        "VerifyEmail", "Account",
                        new { email = model.Email, token = encodedToken },
                        Request.Scheme);

                    var verifyNotification = Notification.Create(
                        user.GuidId,
                        "Verify Your Email",
                        "A verification link has been sent to your email address.");
                    verifyNotification.HtmlEmailBody =
                        $"<p>Thanks for registering! Please verify your email to activate your account.</p>" +
                        $"<p><a href='{confirmLink}'>Verify Email Address</a></p>" +
                        $"<p>If you did not register for WildlifeAID, ignore this email.</p>";

                    await _notificationService.SendNotificationAsync(verifyNotification, NotificationType.SystemCritical);

                    _logger.LogInformation("Email verification link queued for {Email}", model.Email);
                }

                // Explicitly redirect to the RegisterConfirmation action on this controller
                return RedirectToAction(nameof(RegisterConfirmation), "Account");
            }

            // If registration fails
            ModelState.AddModelError(
                string.Empty,
                "Registration failed. Please try again.");

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("ForgotPassword")]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Route("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Look up user — intentionally do not reveal whether the email exists
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && !user.IsDeactivated)
            {
                // Rotate the security stamp so any previously issued reset tokens are invalidated
                await _userManager.UpdateSecurityStampAsync(user);

                var rawToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

                var resetLink = Url.Action(
                    "ResetPassword", "Account",
                    new { email = model.Email, token = encodedToken },
                    Request.Scheme);

                var resetNotification = Notification.Create(
                    user.GuidId,
                    "Password Reset Requested",
                    "A password reset link has been sent to your email address.");
                resetNotification.HtmlEmailBody =
                    $"<p>We received a request to reset the password for your WildlifeAID account.</p>" +
                    $"<p><a href='{resetLink}'>Click here to reset your password</a></p>" +
                    $"<p>This link expires shortly. If you did not request a reset, ignore this email.</p>";

                await _notificationService.SendNotificationAsync(resetNotification, NotificationType.SystemCritical);

                _logger.LogInformation("Password reset email queued for {Email}", model.Email);
            }

            // Always show the same response to prevent account enumeration
            model.EmailSent = true;
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("ResetPassword")]
        public async Task<IActionResult> ResetPassword(string? email, string? token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(ForgotPassword));

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return View("ResetPasswordInvalid");

            // Validate the token eagerly so users see an error immediately rather than
            // filling in the form only to get rejected on POST.
            try
            {
                var rawToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                var isValid = await _userManager.VerifyUserTokenAsync(
                    user,
                    _userManager.Options.Tokens.PasswordResetTokenProvider,
                    "ResetPassword",
                    rawToken);
                if (!isValid)
                    return View("ResetPasswordInvalid");
            }
            catch
            {
                return View("ResetPasswordInvalid");
            }

            var vm = new ResetPasswordViewModel { Email = email, Token = token };
            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Route("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var rawToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            var ok = await _authService.ResetPasswordWithTokenAsync(model.Email, rawToken, model.NewPassword);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty,
                    "The reset link is invalid or has expired. Please request a new one.");
                return View(model);
            }

            _logger.LogInformation("Password successfully reset for {Email}", model.Email);
            TempData["PasswordResetSuccess"] = "Your password has been reset. Please log in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        /// <summary>
        /// Test-only endpoint: generates a fresh password reset link for the given email and
        /// returns it as plain text. Only available when the EnableEmailTestEndpoint feature
        /// flag is true.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [Route("GeneratePasswordResetLink")]
        public async Task<IActionResult> GeneratePasswordResetLink(string email)
        {
            if (!_featureFlags.IsEnabled("EnableEmailTestEndpoint"))
                return NotFound();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound("User not found.");

            // Rotate the security stamp so any previously issued reset tokens are invalidated
            await _userManager.UpdateSecurityStampAsync(user);

            var rawToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

            var link = Url.Action(
                "ResetPassword", "Account",
                new { email, token = encodedToken },
                Request.Scheme);

            return Content(link!, "text/plain");
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("RegisterConfirmation")]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail(string? email, string? token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(Register));

            var rawToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var ok = await _authService.ConfirmEmailAsync(email, rawToken);

            ViewBag.Success = ok;
            ViewBag.Email   = email;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("ResendVerification")]
        public IActionResult ResendVerification()
        {
            var model = new ResendVerificationViewModel();
            if (TempData["UnverifiedEmail"] is string email)
                model.Email = email;
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Route("ResendVerification")]
        public async Task<IActionResult> ResendVerification(ResendVerificationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && !user.EmailConfirmed)
            {
                var rawToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
                var confirmLink = Url.Action(
                    "VerifyEmail", "Account",
                    new { email = model.Email, token = encodedToken },
                    Request.Scheme);

                var resendNotification = Notification.Create(
                    user.GuidId,
                    "Verify Your Email",
                    "A new verification link has been sent to your email address.");
                resendNotification.HtmlEmailBody =
                    $"<p>Here is your new verification link:</p>" +
                    $"<p><a href='{confirmLink}'>Verify Email Address</a></p>";

                await _notificationService.SendNotificationAsync(resendNotification, NotificationType.SystemCritical);

                _logger.LogInformation("Verification email re-sent for {Email}", model.Email);
            }

            // Always show the same response — no account enumeration
            model.EmailSent = true;
            return View(model);
        }

        /// <summary>
        /// Test-only endpoint: generates a fresh email-confirmation link for the given address
        /// and returns it as plain text. Gated by EnableEmailTestEndpoint feature flag.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [Route("GenerateEmailConfirmationLink")]
        public async Task<IActionResult> GenerateEmailConfirmationLink(string email)
        {
            if (!_featureFlags.IsEnabled("EnableEmailTestEndpoint"))
                return NotFound();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound("User not found.");

            var rawToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
            var link = Url.Action(
                "VerifyEmail", "Account",
                new { email, token = encodedToken },
                Request.Scheme);

            return Content(link!, "text/plain");
        }

        [HttpGet]
        [Route("Deactivate")]
        public IActionResult Deactivate()
        {
            return View(new DeactivateAccountViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Deactivate")]
        public async Task<IActionResult> Deactivate(DeactivateAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get the user from the claims principal directly
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.Email == null)
            {
                return RedirectToAction("Login");
            }

            // model.Password holds the user's
            //   password input from the DeactivateAccountViewModel.

            // Check that the input password is correct
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.Password);

            if (!isPasswordCorrect)
            {
                // Highlight the error in the Password field for UI feedback
                ModelState.AddModelError("Password", "The password provided is incorrect.");
                return View(model);
            }

            // Input password matches, deactivate the account.
            var result = await _authService.DeactivateAccountAsync(user.Email);

            if (!result)
            {
                ModelState.AddModelError("Password", "Incorrect password");
                return View(model);
            }

            await _authService.SignOutUserAsync(HttpContext);
            TempData["SuccessMessage"] = "Your account has been deactivated.";
            return RedirectToAction("Login");
        }

        // CSP-168: Forced display name setup for users whose DisplayName is "UNSET".
        [HttpGet]
        [Route("SetDisplayName")]
        public IActionResult SetDisplayName()
        {
            return View(new SetDisplayNameViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("SetDisplayName")]
        public async Task<IActionResult> SetDisplayName(SetDisplayNameViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction(nameof(Login));

            await _userService.UpdateDisplayNameAsync(user, model.DisplayName);
            _logger.LogInformation("User {Email} set display name to '{DisplayName}'", user.Email, model.DisplayName);

            return RedirectToAction("Index", "Dashboard");
        }

        // Logs the current user out and clears authentication cookies.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await _authService.SignOutUserAsync(HttpContext);

            // Log logout event
            _logger.LogInformation("User logged out");

            // Redirect to home page after logout
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        [AllowAnonymous]
        [Route("Reactivate")]
        public IActionResult Reactivate()
        {
            var model = new ReactivateAccountViewModel();

            // Pre-fill email if coming from login page
            if (TempData["DeactivatedEmail"] is string email)
            {
                model.Email = email;
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Route("Reactivate")]
        public async Task<IActionResult> Reactivate(ReactivateAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if user exists
            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Account not found.");
                return View(model);
            }

            // Check if account is actually deactivated
            if (!user.IsDeactivated)
            {
                ModelState.AddModelError(string.Empty, "This account is already active. Please log in.");
                return View(model);
            }

            // Attempt to reactivate
            var result = await _authService.ReactivateAccountAsync(model.Email, model.Password);
            if (!result)
            {
                ModelState.AddModelError("Password", "Incorrect password");
                return View(model);
            }

            // Sign the user in after reactivation
            await _authService.SignInUserAsync(HttpContext, model.Email, rememberMe: false);

            _logger.LogInformation("Account {Email} reactivated and signed in", model.Email);

            TempData["SuccessMessage"] = "Your account has been reactivated!";
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
