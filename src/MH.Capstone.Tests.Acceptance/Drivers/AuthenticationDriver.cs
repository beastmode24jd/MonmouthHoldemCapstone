using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Tests.Acceptance.PageObjects;
using Microsoft.AspNetCore.Http.Features.Authentication;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Drivers
{
    [ExcludeFromCodeCoverage]
    public class AuthenticationDriver
    {
        private readonly IWebDriver _webDriver;

        public AuthenticationDriver(IWebDriver webDriver)
        {
            _webDriver = webDriver;
        }

        public bool CheckIfLoggedIn(string? username = null)
        {
            // TODO - Put this URL in a config file or something similar so that it can be run against different environments
            _webDriver.Navigate().GoToUrl("https://localhost:7147");
            
            try
            {
                var userElement = _webDriver.FindElement(By.Id("userDropdownNavDisplay"));
                return string.IsNullOrEmpty(username) || userElement.Text.Contains(username);
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public void PreformLoginForUser(string username, string password)
        {
            // Check if the user is already logged in
            if (CheckIfLoggedIn(username))
            {
                return;
            }

            // If logged-in user is a different user, log out the current user first
            if (CheckIfLoggedIn())
            {
                var userDropdown = _webDriver.FindElement(By.Id("userDropdownNavDisplay"));
                userDropdown.Click();
                var logoutBtn = _webDriver.FindElement(By.Id("logoutBtn"));
                logoutBtn.Click();
            }

            // Log in the user using the login page
            var loginPage = new LoginPageObject(_webDriver);

            loginPage.UsernameInput.SendKeys(username);
            loginPage.PasswordInput.SendKeys(password);
            loginPage.SubmitBtn.Click();

            // Verify that the user is now logged in, throwing an exception if not
            if (!CheckIfLoggedIn(username))
            {
                throw new Exception($"Failed to log in user '{username}'.");
            }
        }
    }
}
