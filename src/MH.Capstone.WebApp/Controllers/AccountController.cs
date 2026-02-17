using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize]
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authService;
        private readonly UserManager<ApplicationUser> _userManager;

        // Logger for tracking authentication-related events
        private readonly ILogger<AccountController> _logger;

        // Constructor: injects authentication service and logger via dependency injection
        public AccountController(
            IAuthenticationService authService,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _userManager = userManager;
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
            }

            return View(vm);
        }

        // Displays the login page.
        // If the user is already authenticated, they are redirected to the dashboard.     
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

        // Processes login form submission.
        // Validates credentials and signs the user in if successful.
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

            // Validate user credentials against authentication service
            var isValid = await _authService.ValidateCredentialsAsync(
                model.Email,
                model.Password);

            // If credentials are valid, sign the user in
            if (isValid)
            {
                await _authService.SignInUserAsync(
                    HttpContext,
                    model.Email,
                    model.RememberMe);

                // Log successful login
                _logger.LogInformation(
                    "User {Email} logged in successfully",
                    model.Email);

                // Redirect to return URL if provided and safe
                if (!string.IsNullOrEmpty(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Default redirect after login
                return RedirectToAction("Index", "Dashboard");
            }

            // Add generic error message for failed login attempt
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(model);
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
            if (await _authService.UserExistsAsync(model.Email))
            {
                // Log specific reason server-side to avoid email enumeration in responses
                _logger.LogWarning(
                    "Registration attempt with already registered email: {Email}",
                    model.Email);

                ModelState.AddModelError(
                    string.Empty,
                    "Registration failed. Please try again.");

                return View(model);
            }

            // Attempt to register the user
            var success = await _authService.RegisterUserAsync(
                model.Email,
                model.Password);

            // If registration succeeds, sign the user in
            if (success)
            {
                _logger.LogInformation(
                    "New user registered: {Email}",
                    model.Email);

                await _authService.SignInUserAsync(
                    HttpContext,
                    model.Email,
                    rememberMe: false);

                return RedirectToAction("Index", "Dashboard");
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
            model.Identifier = (model.Identifier ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(model.Identifier))
            {
                ModelState.AddModelError(nameof(model.Identifier), "Email is required.");
                return View(model);
            }

            var exists = await _authService.UserExistsAsync(model.Identifier);

            if (!exists)
            {
                ModelState.AddModelError(string.Empty, "We could not find that account. Please try again.");
                model.ShowPasswordResetFields = false;
                return View(model);
            }

            model.ShowPasswordResetFields = true;

            var newPass = model.NewPassword ?? string.Empty;
            var confirm = model.ConfirmNewPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirm))
            {
                return View(model);
            }

            if (!_authService.IsPasswordValid(newPass))
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Password must be at least 8 characters and include an uppercase letter, lowercase letter, number, and symbol.");
                return View(model);
            }

            if (!string.Equals(newPass, confirm, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.ConfirmNewPassword), "The two passwords do not match.");
                return View(model);
            }

            var resetOk = await _authService.ResetPasswordAsync(model.Identifier, newPass);

            if (!resetOk)
            {
                ModelState.AddModelError(string.Empty, "We could not reset your password. Please try again.");
                return View(model);
            }

            TempData["PasswordResetSuccess"] = "Your password was changed. Please log in.";
            return RedirectToAction(nameof(Login));
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

            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var result = await _authService.DeactivateAccountAsync(email, model.Password);
            if (!result)
            {
                ModelState.AddModelError("Password", "Incorrect password");
                return View(model);
            }

            await _authService.SignOutUserAsync(HttpContext);
            TempData["SuccessMessage"] = "Your account has been deactivated.";
            return RedirectToAction("Login");
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
    }
}
