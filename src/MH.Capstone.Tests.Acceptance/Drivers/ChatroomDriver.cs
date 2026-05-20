using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class ChatroomDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public ChatroomDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    /// <summary>Navigates to the Chatroom page for the club whose detail page is currently open.</summary>
    public void NavigateToChatroomFromClubPage()
    {
        var chatroomLink = _wait.Until(d =>
            d.FindElements(By.CssSelector("a.btn[href*='/Clubs/Chatroom/']"))
             .FirstOrDefault(e => e.Displayed));

        chatroomLink?.Click();
        TestContext.Out.WriteLine($"[{nameof(ChatroomDriver)}] Clicked 'Go to Chatroom' link.");

        _wait.Until(d => d.Url.Contains("/Clubs/Chatroom/", StringComparison.OrdinalIgnoreCase));
        TestContext.Out.WriteLine($"[{nameof(ChatroomDriver)}] Arrived on chatroom: {_webDriver.Url}");
    }

    /// <summary>
    /// Extracts the club GUID from the current URL and returns the matching Chatroom URL.
    /// Works from any page whose URL contains a GUID segment (ClubPage, Chatroom, etc.).
    /// </summary>
    public string GetChatroomUrl()
    {
        var url = _webDriver.Url;
        // The GUID is always the last path segment.
        var lastSegment = url.TrimEnd('/').Split('/').Last();
        if (!Guid.TryParse(lastSegment, out var clubId))
            throw new InvalidOperationException($"Cannot parse club GUID from URL: {url}");

        return $"{_baseUrl}/Clubs/Chatroom/{clubId}";
    }

    /// <summary>Navigates to a chatroom by absolute URL.</summary>
    public void NavigateToUrl(string url)
    {
        _webDriver.Navigate().GoToUrl(url);
        TestContext.Out.WriteLine($"[{nameof(ChatroomDriver)}] Navigated to {url}");
    }

    /// <summary>Returns true when the browser is on a Chatroom page URL.</summary>
    public bool IsOnChatroomPage()
    {
        try
        {
            return _wait.Until(d =>
                d.Url.Contains("/Clubs/Chatroom/", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true when <c>#emptyMessagesState</c> is visible — i.e. there are no messages yet.
    /// </summary>
    public bool IsEmptyStateVisible()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
            {
                var el = d.FindElements(By.Id("emptyMessagesState")).FirstOrDefault();
                return el != null && el.Displayed;
            });
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Types <paramref name="content"/> into the message input and submits the form.
    /// Waits for the message to appear in the message list (delivered via SignalR, no page reload).
    /// </summary>
    public void SendMessage(string content)
    {
        var input = _wait.Until(d => d.FindElement(By.Id("messageInput")));
        input.Clear();
        input.SendKeys(content);
        TestContext.Out.WriteLine($"[{nameof(ChatroomDriver)}] Typed message: '{content}'");

        var btn = _wait.Until(d => d.FindElement(By.Id("sendMessageBtn")));
        btn.Click();
        TestContext.Out.WriteLine($"[{nameof(ChatroomDriver)}] Clicked send button.");
    }

    /// <summary>
    /// Returns true when text matching <paramref name="content"/> appears anywhere inside
    /// <c>#messageList</c>. Uses a short poll since delivery is via SignalR (no page reload).
    /// </summary>
    public bool IsMessageVisible(string content)
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10)).Until(d =>
            {
                var list = d.FindElements(By.Id("messageList")).FirstOrDefault();
                return list != null && list.Text.Contains(content, StringComparison.OrdinalIgnoreCase);
            });
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true when the browser URL does NOT contain "/Clubs/Chatroom/" —
    /// meaning the user was redirected away (access denied or login redirect).
    /// </summary>
    public bool WasRedirectedAwayFromChatroom()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
                !d.Url.Contains("/Clubs/Chatroom/", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
