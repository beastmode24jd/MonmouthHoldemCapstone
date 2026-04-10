using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class SightingsDriver
{
    private readonly IWebDriver _webDriver;
    private readonly string _baseUrl;

    public SightingsDriver(IWebDriver webDriver, AcceptanceTestSettings settings)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    public void NavigateToSightingsUpload() =>
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Sighting/Create");
}
