using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

/// <summary>
/// Driver for the sightings map view (<c>/Map</c>). Provides navigation,
/// JSON-endpoint fetching with cookie injection, and Leaflet pan control
/// via the <c>window.__sightingsMap</c> test handle exposed by the view.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public MapDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToMap()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Map");
        _wait.Until(d => ((IJavaScriptExecutor)d)
            .ExecuteScript("return typeof window.__sightingsMap !== 'undefined' && window.__sightingsMap !== null;")
            is bool b && b);
    }

    /// <summary>
    /// Overwrite the UserTimeZone cookie. site.js sets it on every page load,
    /// so call this AFTER navigating to /Map and BEFORE FetchSightingsRaw.
    /// </summary>
    public void SetUserTimeZoneCookie(string ianaId)
    {
        _webDriver.Manage().Cookies.DeleteCookieNamed("UserTimeZone");
        _webDriver.Manage().Cookies.AddCookie(new Cookie("UserTimeZone", ianaId, "/", null));
    }

    /// <summary>
    /// Calls the /Map/Sightings JSON endpoint from the page context (so the
    /// current cookies are sent) and returns the raw response body as a string.
    /// </summary>
    public string FetchSightingsRaw(double minLat, double maxLat, double minLng, double maxLng)
    {
        var script = @"
            var done = arguments[arguments.length - 1];
            var url = '/Map/Sightings?minLat=' + arguments[0] + '&maxLat=' + arguments[1] +
                      '&minLng=' + arguments[2] + '&maxLng=' + arguments[3];
            fetch(url, { credentials: 'same-origin' })
                .then(function (r) { return r.text(); })
                .then(function (t) { done(t); })
                .catch(function (e) { done('ERROR:' + e.message); });
        ";
        var js = (IJavaScriptExecutor)_webDriver;
        var result = js.ExecuteAsyncScript(script, minLat, maxLat, minLng, maxLng);
        return result?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Programmatically pan the Leaflet map. With the world-bounds clamp the
    /// effective center will be re-snapped into [-180, 180] by Leaflet.
    /// </summary>
    public void PanTo(double lat, double lng)
    {
        var js = (IJavaScriptExecutor)_webDriver;
        js.ExecuteScript("window.__sightingsMap.panTo([arguments[0], arguments[1]], { animate: false });", lat, lng);
    }

    public double GetCenterLongitude()
    {
        var js = (IJavaScriptExecutor)_webDriver;
        // Convert.ToDouble — ExecuteScript may return Int64 for a clean integer; hard cast flakes.
        var raw = js.ExecuteScript("return window.__sightingsMap.getCenter().lng;");
        return Convert.ToDouble(raw);
    }
}
