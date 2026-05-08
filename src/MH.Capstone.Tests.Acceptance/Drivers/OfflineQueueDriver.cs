using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace MH.Capstone.Tests.Acceptance.Drivers;

/// <summary>
/// CSP-177: Selenium driver for the offline queue page (/Sighting/OfflineQueue).
/// Also provides helpers for simulating offline state via JavaScript injection.
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
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
                d.Url.Contains("/Sighting/OfflineQueue", StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    public bool HasQueuedItems()
    {
        var rows = _webDriver.FindElements(By.CssSelector(".queue-item-row"));
        return rows.Count > 0;
    }

    public bool IsEmptyStateVisible()
    {
        var els = _webDriver.FindElements(By.Id("offlineQueueEmpty"));
        return els.Count > 0 && els[0].Displayed;
    }

    public bool HasDeleteButtons() =>
        _webDriver.FindElements(By.CssSelector(".deleteQueueItemBtn")).Count > 0;

    /// <summary>Injects a queued sighting directly into IndexedDB via JavaScript for the current user.</summary>
    public void InjectQueuedSighting(string speciesName = "Test Species CSP-177")
    {
        var js = (IJavaScriptExecutor)_webDriver;
        var userIdEl = _webDriver.FindElements(By.Id("currentUserId")).FirstOrDefault();
        var userId = userIdEl?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(userId)) return;

        // Call the exported enqueueOfflineSighting function from offline-queue.js
        js.ExecuteScript(@"
            (async function(userId, speciesName) {
                await enqueueOfflineSighting(userId, {
                    speciesName: speciesName,
                    latitude: '45.00000',
                    longitude: '-123.00000',
                    timestamp: new Date(Date.now() - 60000).toISOString().slice(0, 16),
                    timezone: 'America/Los_Angeles',
                    description: 'CSP-177 acceptance test sighting.',
                    imageDataUrl: 'data:image/jpeg;base64,/9j/4AAQSkZJRg==',
                    imageFileName: 'test.jpg',
                    clientSightingId: crypto.randomUUID()
                });
            })(arguments[0], arguments[1]);
        ", userId, speciesName);

        // Short wait for IndexedDB write
        System.Threading.Thread.Sleep(500);
        _webDriver.Navigate().Refresh();
        WaitForPageReady();
    }

    /// <summary>Simulates offline state by overriding navigator.onLine to false.</summary>
    public void SimulateOffline()
    {
        var js = (IJavaScriptExecutor)_webDriver;
        js.ExecuteScript(@"
            Object.defineProperty(navigator, 'onLine', { get: function() { return false; }, configurable: true });
            window.dispatchEvent(new Event('offline'));
        ");
    }

    /// <summary>Restores online state and fires the online event to trigger auto-sync.</summary>
    public void SimulateOnline()
    {
        var js = (IJavaScriptExecutor)_webDriver;
        js.ExecuteScript(@"
            Object.defineProperty(navigator, 'onLine', { get: function() { return true; }, configurable: true });
            window.dispatchEvent(new Event('online'));
        ");
    }

    /// <summary>
    /// Fills and submits the sighting upload form with a real temp image.
    /// Returns the temp image path so callers can clean it up.
    /// </summary>
    public string SubmitSightingWhileOffline(SightingsDriver sightingsDriver)
    {
        _tempImagePath = Path.Combine(Path.GetTempPath(), $"csp177_{Guid.NewGuid():N}.jpg");
        using (var image = new Image<Rgba32>(1280, 960, new Rgba32(128, 128, 128, 255)))
        using (var fs = File.Create(_tempImagePath))
        {
            image.Save(fs, new JpegEncoder());
        }

        sightingsDriver.NavigateToSightingsUpload();
        SimulateOffline();

        sightingsDriver.SetImageForUpload(_tempImagePath);
        sightingsDriver.SetSpeciesName("CSP177-TestSpecies");
        sightingsDriver.SetLatitude(45.0);
        sightingsDriver.SetLongitude(-123.0);
        sightingsDriver.SetTimestamp(DateTimeOffset.Now.AddMinutes(-1));
        sightingsDriver.SetDescription("CSP-177 offline queue test.");
        sightingsDriver.SubmitSightingsForm();

        WaitForPageReady();
        return _tempImagePath;
    }

    public void CleanupTempImage()
    {
        if (_tempImagePath != null && File.Exists(_tempImagePath))
            File.Delete(_tempImagePath);
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
