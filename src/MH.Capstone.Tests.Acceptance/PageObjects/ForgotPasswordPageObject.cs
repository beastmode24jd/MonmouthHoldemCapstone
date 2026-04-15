using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class ForgotPasswordPageObject
{
    private readonly IWebDriver _driver;

    public IWebElement EmailInput      => _driver.FindElement(By.Id("forgotPasswordEmail"));
    public IWebElement SendResetBtn    => _driver.FindElement(By.Id("sendResetEmailBtn"));
    public IWebElement EmailSentBanner => _driver.FindElement(By.Id("resetEmailSentMessage"));

    public ForgotPasswordPageObject(IWebDriver driver, string baseUrl)
    {
        _driver = driver;
        var url = $"{baseUrl.TrimEnd('/')}/Account/ForgotPassword";
        if (!string.Equals(driver.Url, url, StringComparison.InvariantCultureIgnoreCase))
            driver.Navigate().GoToUrl(url);
    }
}
