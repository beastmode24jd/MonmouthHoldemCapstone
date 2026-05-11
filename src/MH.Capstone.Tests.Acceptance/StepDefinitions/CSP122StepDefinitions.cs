using FluentAssertions;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using MH.Capstone.Tests.Acceptance.Helpers;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions
{
    [Binding]
    [Scope(Tag = "photo-quality")]
    public class CSP122StepDefinitions
    {
        private const string AlexEmail = "alex@test.com";

        private readonly IWebDriver _webDriver;
        private readonly WebDriverWait _wait;
        private readonly SightingsDriver _sightingsDriver;
        private readonly string _baseUrl;
        private readonly string _dashboardUrl;

        private string? _generatedImagePath;
        private string? _currentSightingDescription;

        // CSP-189: tracks whether the When-step submission was expected to succeed.
        // High-quality images redirect to the dashboard; Low-tier images stay on the form.
        private bool _expectedRedirect;

        public CSP122StepDefinitions(IWebDriver webDriver, WebDriverWait wait,
            AcceptanceTestSettings settings, SightingsDriver sightingsDriver)
        {
            _webDriver = webDriver;
            _wait = wait;
            _sightingsDriver = sightingsDriver;
            _baseUrl = settings.BaseUrl.TrimEnd('/');
            _dashboardUrl = $"{_baseUrl}/Dashboard";
        }

        [BeforeScenario("photo-quality")]
        public void ResetSessionCookies()
        {
            _webDriver.Navigate().GoToUrl(_baseUrl);
            _webDriver.Manage().Cookies.DeleteAllCookies();
        }

        [AfterScenario("photo-quality")]
        public void CleanupGeneratedImage()
        {
            if (_generatedImagePath is not null && File.Exists(_generatedImagePath))
                File.Delete(_generatedImagePath);
        }

        [Given("Alex is on the Sighting Upload page")]
        public void GivenAlexIsOnTheSightingUploadPage()
        {
            _sightingsDriver.NavigateToSightingsUpload();
        }

        // ---------- When ----------

        [When("Alex submits a sighting with a {string} image")]
        [When("Alex submits a sighting with an {string} image")]
        public void WhenAlexSubmitsASightingWithAnImage(string imageQuality)
        {
            _generatedImagePath = TestImageFactory.CreateByQuality(imageQuality);
            _expectedRedirect = string.Equals(imageQuality, "high-quality", StringComparison.OrdinalIgnoreCase);

            _currentSightingDescription = $"CSP-122 {imageQuality} {Guid.NewGuid():N}";

            _sightingsDriver.SetLatitude(44.847600);
            _sightingsDriver.SetLongitude(-123.234300);
            _sightingsDriver.SetTimestamp(DateTimeOffset.Now.AddDays(-1));
            _sightingsDriver.SetDescription(_currentSightingDescription);
            _sightingsDriver.SetSpeciesName("Test Species");

            _sightingsDriver.UploadFileAndSubmit(_generatedImagePath);

            if (_expectedRedirect)
            {
                // High-quality submissions redirect to /Dashboard.
                _wait.Until(d => d.Url.Contains("/Dashboard", StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                // Low-tier submissions stay on /Sighting/Upload and render validation errors.
                _wait.Until(d => d.Url.Contains("/Sighting", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        // ---------- Then ----------

        // CSP-189: Low-tier uploads are rejected at the form with a ModelState error
        // that renders inside the <div asp-validation-summary="All"> element.
        [Then("Alex should see the upload error mentioning {string}")]
        public void ThenAlexShouldSeeUploadErrorMentioning(string substring)
        {
            var summary = _wait.Until(d =>
            {
                var el = d.FindElement(By.CssSelector("div[asp-validation-summary], div.validation-summary-errors, .text-danger"));
                return string.IsNullOrWhiteSpace(el.Text) ? null : el;
            });

            // The simplest reliable assertion: any text-danger element on the page contains the reason.
            var dangerTexts = _webDriver.FindElements(By.CssSelector(".text-danger"))
                .Select(e => e.Text ?? string.Empty)
                .ToList();

            dangerTexts.Any(t => t.Contains(substring, StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue($"expected an upload error mentioning '{substring}', got: {string.Join(" | ", dangerTexts)}");
        }

        [Then("the upload stays on the Sighting Upload page")]
        public void ThenUploadStaysOnSightingUploadPage()
        {
            _webDriver.Url.Should().Contain("/Sighting",
                "Low-tier submissions should return to the upload form, not redirect to the dashboard");
            _webDriver.Url.Should().NotContain("/Dashboard");
        }

        [Then("no sighting was saved for that upload")]
        public void ThenNoSightingWasSavedForThatUpload()
        {
            using var scope = GetServiceScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var found = dbContext.Set<Sighting>()
                .AsNoTracking()
                .Any(s => s.Description == _currentSightingDescription);

            found.Should().BeFalse(
                $"no sighting should be saved when the photo is rejected (looked up by description '{_currentSightingDescription}')");
        }

        [Then("Alex should see the success message {string}")]
        public void ThenAlexShouldSeeSuccessMessage(string expectedMessage)
        {
            var element = _wait.Until(d => d.FindElement(By.Id("photoQualityBadge")));
            element.Text.Should().Contain(expectedMessage);
        }

        [Then("the saved sighting should have QualityTier {string}")]
        public void ThenTheSavedSightingShouldHaveQualityTier(string expectedTier)
        {
            var expected = Enum.Parse<PhotoQualityTier>(expectedTier);
            var sighting = GetMostRecentSightingForAlex();
            sighting.QualityTier.Should().Be(expected);
        }

        // ---------- Helpers ----------

        private Sighting GetMostRecentSightingForAlex()
        {
            using var scope = GetServiceScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var alex = dbContext.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Email == AlexEmail)
                ?? throw new InvalidOperationException(
                    $"Acceptance seed missing: no AspNetUser with Email '{AlexEmail}'.");

            var sighting = dbContext.Set<Sighting>()
                .AsNoTracking()
                .Where(s => s.UserIdentityId == alex.Id && s.Description == _currentSightingDescription)
                .FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"No sighting found for {AlexEmail} with description '{_currentSightingDescription}'.");

            return sighting;
        }

        private static IServiceScope GetServiceScope()
        {
            var appServices = TestWebAppHost.Services
                ?? throw new InvalidOperationException(
                    "TestWebAppHost has not started; cannot resolve web app services.");
            return appServices.CreateScope();
        }
    }
}
