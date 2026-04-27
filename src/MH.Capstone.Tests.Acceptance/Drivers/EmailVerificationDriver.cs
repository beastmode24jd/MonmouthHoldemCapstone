using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class EmailVerificationDriver
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public EmailVerificationDriver(IWebDriver driver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _driver = driver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    /// <summary>
    /// Registers a new user via the registration form and waits for the result page.
    /// </summary>
    public void RegisterNewUser(string email, string password, string displayName = "TestUser")
    {
        // Ensure no user is currently logged in to avoid immediately redirecting away from the Register page.
        try
        {
            _driver.Navigate().GoToUrl(_baseUrl);
            var logoutForms = _driver.FindElements(By.Id("logoutForm"));
            if (logoutForms.Count > 0)
            {
                try { logoutForms[0].Submit(); } catch { }
            }
        }
        catch { /* ignore */ }

        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Register");
        try
        {
            _wait.Until(d =>
            {
                try
                {
                    var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                    return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            });
        }
        catch (OpenQA.Selenium.WebDriverTimeoutException ex)
        {
            // Some environments can leave readyState unsettled while the page is actually usable.
            // Log and continue — the explicit element waits below will catch missing fields.
            TestContext.Out.WriteLine($"[{nameof(EmailVerificationDriver)}] Warning: document.readyState wait timed out: {ex.Message}");
        }

        _wait.Until(d => d.FindElement(By.Id("displayNameField"))).SendKeys(displayName);
        _wait.Until(d => d.FindElement(By.Id("emailField"))).SendKeys(email);

        // The password field has id="passwordField" in Register.cshtml
        _wait.Until(d => d.FindElement(By.Id("passwordField"))).SendKeys(password);
        _wait.Until(d => d.FindElement(By.Id("confirmPasswordField"))).SendKeys(password);

        // JS enables the submit button only when all fields are valid
        _wait.Until(d =>
        {
            var btn = d.FindElements(By.Id("submitBtn"));
            return btn.Count > 0 && btn[0].Enabled;
        });

        _wait.Until(d => d.FindElement(By.Id("submitBtn"))).Click();
        // Wait explicitly for the RegisterConfirmation page element rather than just
        // document.readyState — the latter can fire on the Register page before the
        // 302 redirect to RegisterConfirmation has started, causing a race condition
        // where subsequent GoToUrl calls fight with the pending redirect.
        _wait.Until(d =>
            d.FindElements(By.Id("registrationConfirmationMessage")).Count > 0);
    }

    /// <summary>Returns true when the "check your email" registration confirmation page is shown.</summary>
    public bool IsRegistrationConfirmationVisible()
    {
        try
        {
            return _wait.Until(d =>
                d.FindElements(By.Id("registrationConfirmationMessage")).Count > 0);
        }
        catch { return false; }
    }

    /// <summary>
    /// Calls the test-only endpoint to generate an email confirmation link for the given address.
    /// </summary>
    public string GetEmailConfirmationLink(string email)
    {
        var url = $"{_baseUrl}/Account/GenerateEmailConfirmationLink?email={Uri.EscapeDataString(email)}";
        _driver.Navigate().GoToUrl(url);
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });
        var link = _wait.Until(d => d.FindElement(By.TagName("body"))).Text.Trim();
        TestContext.Out.WriteLine($"[{nameof(EmailVerificationDriver)}] Confirmation link for {email}: {link}");
        return link;
    }

    /// <summary>Navigates to the given verification link.</summary>
    public void NavigateToVerificationLink(string link)
    {
        _driver.Navigate().GoToUrl(link);
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });
    }

    /// <summary>Navigates to a VerifyEmail URL with a syntactically valid but wrong token.</summary>
    public void NavigateToInvalidVerificationLink(string email)
    {
        // Use a Base64Url string that decodes to garbage — Identity will reject the token
        var fakeToken = "aW52YWxpZC12ZXJpZnktdG9rZW4";
        var path = $"Account/VerifyEmail?email={Uri.EscapeDataString(email)}&token={fakeToken}";
        _driver.Navigate().GoToUrl($"{_baseUrl}/{path}");
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });
    }

    /// <summary>Returns true when the email-verified success message is visible.</summary>
    public bool IsVerificationSuccessVisible()
    {
        try
        {
            return _wait.Until(d =>
                d.FindElements(By.Id("emailVerifiedSuccessMessage")).Count > 0);
        }
        catch { return false; }
    }

    /// <summary>Returns true when the verification-failed error message is visible.</summary>
    public bool IsVerificationErrorVisible()
    {
        try
        {
            return _wait.Until(d =>
                d.FindElements(By.Id("emailVerificationErrorMessage")).Count > 0);
        }
        catch { return false; }
    }

    /// <summary>Returns true when a "request new verification link" option is present.</summary>
    public bool HasRequestNewVerificationLinkOption()
    {
        var byId   = _driver.FindElements(By.Id("requestNewVerificationBtn"));
        var byText = _driver.FindElements(By.PartialLinkText("new verification link"));
        var byText2 = _driver.FindElements(By.PartialLinkText("Request a New Verification Link"));
        return byId.Count > 0 || byText.Count > 0 || byText2.Count > 0;
    }

    /// <summary>
    /// Tries to log in and returns without asserting — caller checks state after.
    /// </summary>
    public void AttemptLogin(string email, string password)
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Login");
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });

        _wait.Until(d => d.FindElement(By.Id("emailField"))).SendKeys(email);
        _wait.Until(d => d.FindElement(By.Id("passwordField"))).SendKeys(password);

        // Wait for the submit button to be enabled by JS
        _wait.Until(d =>
        {
            var btn = d.FindElements(By.Id("submitBtn"));
            return btn.Count > 0 && btn[0].Enabled;
        });

        _wait.Until(d => d.FindElement(By.Id("submitBtn"))).Click();
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });
    }

    /// <summary>Returns true when the "email not verified" warning banner is shown on the login page.</summary>
    public bool IsEmailVerificationRequiredMessageVisible()
    {
        try
        {
            return _wait.Until(d =>
                d.FindElements(By.Id("emailNotVerifiedMessage")).Count > 0);
        }
        catch { return false; }
    }

    /// <summary>Returns true when the "resend verification" button/link is visible.</summary>
    public bool HasResendVerificationOption()
    {
        var byBtn  = _driver.FindElements(By.Id("resendVerificationBtn"));
        var byText = _driver.FindElements(By.PartialLinkText("Resend Verification"));
        return byBtn.Count > 0 || byText.Count > 0;
    }

    /// <summary>Submits the Resend Verification form with the given email address.</summary>
    public void SubmitResendVerification(string email)
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/ResendVerification");
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });

        var emailInput = _wait.Until(d => d.FindElement(By.Id("resendVerificationEmail")));
        emailInput.Clear();
        emailInput.SendKeys(email);
        _wait.Until(d => d.FindElement(By.Id("resendVerificationSubmitBtn"))).Click();
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });
    }

    /// <summary>Returns true when the resend-confirmation success banner is shown.</summary>
    public bool IsResendConfirmationVisible()
    {
        try
        {
            return _wait.Until(d =>
                d.FindElements(By.Id("resendVerificationSentMessage")).Count > 0);
        }
        catch { return false; }
    }
}
