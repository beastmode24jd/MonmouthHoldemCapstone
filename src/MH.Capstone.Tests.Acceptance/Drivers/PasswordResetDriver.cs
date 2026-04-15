using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Helpers;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class PasswordResetDriver
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;

    public PasswordResetDriver(IWebDriver driver, AcceptanceTestSettings settings)
    {
        _driver = driver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Navigates to the Forgot Password page and submits the given email address.
    /// </summary>
    public void SubmitForgotPasswordRequest(string email)
    {
        var page = new ForgotPasswordPageObject(_driver, _baseUrl);
        page.EmailInput.Clear();
        page.EmailInput.SendKeys(email);
        page.SendResetBtn.Click();
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Returns true when the "check your email" confirmation banner is visible.
    /// </summary>
    public bool IsEmailSentBannerVisible()
    {
        try
        {
            return _driver.WaitUntil(d =>
            {
                var elems = d.FindElements(By.Id("resetEmailSentMessage"));
                return elems.Count > 0 && elems[0].Displayed;
            }, TimeSpan.FromSeconds(5));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Calls the test-only endpoint to obtain a password reset URL for the given email.
    /// Requires EnableEmailTestEndpoint = true in FeatureFlags.
    /// </summary>
    public string GetPasswordResetLink(string email)
    {
        var testUrl = $"{_baseUrl}/Account/GeneratePasswordResetLink?email={Uri.EscapeDataString(email)}";
        _driver.Navigate().GoToUrl(testUrl);
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
        var link = _driver.FindElement(By.TagName("body")).Text.Trim();
        TestContext.Out.WriteLine($"[{nameof(PasswordResetDriver)}] Reset link for {email}: {link}");
        return link;
    }

    /// <summary>
    /// Navigates to a reset link and fills in + submits the new password form.
    /// If the token is already invalid the browser lands on the error page; in that
    /// case this method returns without attempting to interact with a non-existent form.
    /// </summary>
    public void NavigateToResetLinkAndSubmit(string resetLink, string newPassword, string confirmPassword)
    {
        _driver.Navigate().GoToUrl(resetLink);
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));

        // If the token was already invalidated we land on the error page — no form to fill.
        if (_driver.FindElements(By.Id("invalidResetLinkMessage")).Count > 0)
            return;

        var page = new ResetPasswordPageObject(_driver);
        page.NewPasswordInput.Clear();
        page.NewPasswordInput.SendKeys(newPassword);
        page.ConfirmPasswordInput.Clear();
        page.ConfirmPasswordInput.SendKeys(confirmPassword);
        page.ResetPasswordBtn.Click();
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Navigates to a reset link only — does not interact with the form.
    /// </summary>
    public void NavigateToResetLink(string resetLink)
    {
        _driver.Navigate().GoToUrl(resetLink);
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Navigates to a relative path under the base URL.
    /// </summary>
    public void NavigateToPath(string relativePath)
    {
        var url = $"{_baseUrl}/{relativePath.TrimStart('/')}";
        _driver.Navigate().GoToUrl(url);
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Returns true when the password reset form is visible on the current page.
    /// </summary>
    public bool IsResetPasswordFormVisible()
    {
        try
        {
            return _driver.WaitUntil(d =>
                d.FindElements(By.Id("resetPasswordBtn")).Count > 0,
                TimeSpan.FromSeconds(5));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true when the "invalid reset link" error page is shown.
    /// </summary>
    public bool IsInvalidLinkPageVisible()
    {
        try
        {
            return _driver.WaitUntil(d =>
                d.FindElements(By.Id("invalidResetLinkMessage")).Count > 0,
                TimeSpan.FromSeconds(5));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true when an inline "invalid or expired" error is shown on the reset form.
    /// </summary>
    public bool IsInvalidTokenInlineErrorVisible()
    {
        try
        {
            return _driver.WaitUntil(d =>
            {
                var errDiv = d.FindElements(By.Id("resetPasswordError"));
                return errDiv.Count > 0 && !string.IsNullOrWhiteSpace(errDiv[0].Text);
            }, TimeSpan.FromSeconds(5));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true when the "request new reset link" option is available on the current page.
    /// </summary>
    public bool HasRequestNewLinkOption()
    {
        var byId   = _driver.FindElements(By.Id("requestNewResetLinkBtn"));
        var byText = _driver.FindElements(By.PartialLinkText("new reset link"));
        var byText2 = _driver.FindElements(By.PartialLinkText("Request a New Reset Link"));
        return byId.Count > 0 || byText.Count > 0 || byText2.Count > 0;
    }

    /// <summary>
    /// Returns true when the login page shows the password-reset success banner.
    /// </summary>
    public bool IsPasswordResetSuccessBannerOnLoginPage()
    {
        try
        {
            return _driver.WaitUntil(d =>
            {
                var isLogin = d.Url.Contains("/Account/Login", StringComparison.InvariantCultureIgnoreCase);
                var hasBanner = d.FindElements(By.Id("passwordResetSuccessMessage")).Count > 0;
                return isLogin && hasBanner;
            }, TimeSpan.FromSeconds(5));
        }
        catch
        {
            return false;
        }
    }
}
