using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Support;

public class RobustWebElement : IWebElement
{
    private readonly IWebElement _inner;
    private readonly IWebDriver _driver;

    public RobustWebElement(IWebElement inner, IWebDriver driver)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public string TagName => _inner.TagName;
    public string Text => _inner.Text;
    public bool Enabled => _inner.Enabled;
    public bool Selected => _inner.Selected;
    public Point Location => _inner.Location;
    public Size Size => _inner.Size;
    public bool Displayed => _inner.Displayed;

    public void Clear() => _inner.Clear();

    public void Click()
    {
        try
        {
            _inner.Click();
        }
        catch (Exception)
        {
            // Fallback to JavaScript click when the normal click fails (e.g., intercepted or not clickable)
            if (_driver is IJavaScriptExecutor js)
            {
                try
                {
                    js.ExecuteScript("arguments[0].scrollIntoView(true);", _inner);
                    js.ExecuteScript("arguments[0].click();", _inner);
                    return;
                }
                catch
                {
                    // If JS fallback also fails, rethrow to preserve failure behavior
                    throw;
                }
            }

            throw;
        }
    }

    public IWebElement FindElement(By by) => new RobustWebElement(_inner.FindElement(by), _driver);

    public ReadOnlyCollection<IWebElement> FindElements(By by)
    {
        var elements = _inner.FindElements(by);
        var list = new List<IWebElement>(elements.Count);
        foreach (var e in elements) list.Add(new RobustWebElement(e, _driver));
        return new ReadOnlyCollection<IWebElement>(list);
    }

    public string GetAttribute(string attributeName) => _inner.GetAttribute(attributeName);
    public string GetCssValue(string propertyName) => _inner.GetCssValue(propertyName);
    public string GetProperty(string propertyName) => _inner.GetProperty(propertyName);
    public void SendKeys(string text) => _inner.SendKeys(text);
    public void Submit() => _inner.Submit();
}
