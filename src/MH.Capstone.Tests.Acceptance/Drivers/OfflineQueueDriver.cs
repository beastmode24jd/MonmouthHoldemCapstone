using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace MH.Capstone.Tests.Acceptance.Drivers;

/// <summary>
/// CSP-177: Selenium driver for the offline queue page (/Sighting/OfflineQueue).
/// Simulates offline state via window.__FORCE_OFFLINE (navigator.onLine is
/// non-configurable in Chrome so Object.defineProperty does not work).
/// </summary>
[ExcludeFromCodeCoverage]
public class OfflineQueueDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;
    private string? _tempImagePath;

    public OfflineQueueDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToOfflineQueue()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Sighting/OfflineQueue");
        WaitForPageReady();
    }

    public bool IsOnOfflineQueuePage()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10)).Until(d =>
                d.Url.Contains("/Sighting/OfflineQueue", StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    public bool HasQueuedItems()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
                d.FindElements(By.CssSelector(".queue-item-row")).Count > 0);
        }
        catch { return false; }
    }

    public bool IsEmptyStateVisible()
    {
        var els = _webDriver.FindElements(By.Id("offlineQueueEmpty"));
        return els.Count > 0 && els[0].Displayed;
    }

    public bool HasDeleteButtons() =>
        _webDriver.FindElements(By.CssSelector(".deleteQueueItemBtn")).Count > 0;

    /// <summary>
    /// Sets window.__FORCE_OFFLINE = true so that isOffline() in sighting-upload.js
    /// returns true regardless of navigator.onLine (which Chrome does not allow overriding).
    /// </summary>
    public void SimulateOffline()
    {
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("window.__FORCE_OFFLINE = true;");
    }

    /// <summary>Clears the offline flag and fires the online event to trigger auto-sync.</summary>
    public void SimulateOnline()
    {
        ((IJavaScriptExecutor)_webDriver).ExecuteScript(
            "window.__FORCE_OFFLINE = false; window.dispatchEvent(new Event('online'));");
    }

    /// <summary>
    /// Injects a queued sighting directly into IndexedDB via window.enqueueOfflineSighting,
    /// which offline-queue.js exposes on window for acceptance testing.
    /// </summary>
    public void InjectQueuedSighting(string speciesName = "Test Species CSP-177")
    {
        var js = (IJavaScriptExecutor)_webDriver;
        var userIdEl = _webDriver.FindElements(By.Id("currentUserId")).FirstOrDefault();
        var userId = userIdEl?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(userId)) return;

        // window.enqueueOfflineSighting is exposed by offline-queue.js for Selenium use.
        // Returns a Promise; ExecuteScript with async callback waits for resolution.
        js.ExecuteAsyncScript(@"
            var userId = arguments[0];
            var speciesName = arguments[1];
            var done = arguments[arguments.length - 1];
            window.enqueueOfflineSighting(userId, {
                speciesName: speciesName,
                latitude: '45.00000',
                longitude: '-123.00000',
                timestamp: new Date(Date.now() - 60000).toISOString().slice(0, 16),
                timezone: 'America/Los_Angeles',
                description: 'CSP-177 acceptance test sighting.',
                imageDataUrl: 'data:image/jpeg;base64,/9j/4AAQSkZJRg==',
                imageFileName: 'test.jpg',
                clientSightingId: crypto.randomUUID()
            }).then(function() { done(); }).catch(function(e) { done(e.toString()); });
        ", userId, speciesName);

        _webDriver.Navigate().Refresh();
        WaitForPageReady();
    }

    /// <summary>
    /// Clicks the submit button on the upload form (triggering the JS submit event
    /// handler) and waits for the browser to navigate away from the upload page.
    /// Use this instead of SightingsDriver.SubmitSightingsForm() for offline scenarios
    /// because form.submit() bypasses JS event listeners.
    /// </summary>
    public void ClickSubmitButton()
    {
        var btn = _wait.Until(d => d.FindElement(By.Id("SubmitBtn")));
        btn.Click();
    }

    public void CleanupTempImage()
    {
        if (_tempImagePath != null && File.Exists(_tempImagePath))
            File.Delete(_tempImagePath);
        _tempImagePath = null;
    }

    private void WaitForPageReady()
    {
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
        catch { }
    }
}
