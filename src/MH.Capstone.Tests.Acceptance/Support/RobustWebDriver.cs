using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Support;

public class RobustWebDriver : IWebDriver, IWrapsDriver
{
    private readonly IWebDriver _raw;
    private readonly TimeSpan _timeout;

    public RobustWebDriver(IWebDriver raw, TimeSpan timeout)
    {
        _raw = raw ?? throw new ArgumentNullException(nameof(raw));
        _timeout = timeout;
    }

    public string Url { get => _raw.Url; set => _raw.Url = value; }
    public string Title => _raw.Title;
    public string PageSource => _raw.PageSource;
    public string CurrentWindowHandle => _raw.CurrentWindowHandle;
    public ReadOnlyCollection<string> WindowHandles => _raw.WindowHandles;
    public void Close() => _raw.Close();
    public void Quit() => _raw.Quit();
    public IOptions Manage() => _raw.Manage();
    public INavigation Navigate() => _raw.Navigate();
    public ITargetLocator SwitchTo() => _raw.SwitchTo();
    public void Dispose() => _raw.Dispose();

    public IWebElement FindElement(By by) => new RobustWebElement(_raw.FindElement(by), this);

    public ReadOnlyCollection<IWebElement> FindElements(By by)
    {
        var elements = _raw.FindElements(by);
        var list = new List<IWebElement>(elements.Count);
        foreach (var e in elements) list.Add(new RobustWebElement(e, this));
        return new ReadOnlyCollection<IWebElement>(list);
    }


    // IWrapsDriver
    public IWebDriver WrappedDriver => _raw;
}
