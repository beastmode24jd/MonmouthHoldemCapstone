using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Support
{
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
        public System.Drawing.Point Location => _inner.Location;
        public System.Drawing.Size Size => _inner.Size;
        public bool Displayed => _inner.Displayed;

        public void Clear() => _inner.Clear();

        public void Click()
        {
            try
            {
                _inner.Click();
                return;
            }
            catch (OpenQA.Selenium.ElementClickInterceptedException)
            {
                // continue to fallback
            }
            catch (OpenQA.Selenium.WebDriverException ex) when (ex.Message?.Contains("element click intercepted", StringComparison.OrdinalIgnoreCase) == true)
            {
                // continue to fallback
            }

            // Try scrolling into view and retrying native click
            try
            {
                var js = GetJavaScriptExecutor();
                js?.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", _inner);
            }
            catch { /* ignore */ }

            try
            {
                // small wait loop to allow transient overlays to disappear
                var end = DateTime.UtcNow + TimeSpan.FromSeconds(2);
                while (DateTime.UtcNow < end)
                {
                    try
                    {
                        if (_inner.Displayed && _inner.Enabled)
                        {
                            _inner.Click();
                            return;
                        }
                    }
                    catch { }
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch { }

            // Final fallback: JavaScript click
            var jsFallback = GetJavaScriptExecutor();
            if (jsFallback != null)
            {
                try
                {
                    jsFallback.ExecuteScript("arguments[0].click();", _inner);
                    return;
                }
                catch (Exception e)
                {
                    throw new OpenQA.Selenium.WebDriverException("Robust click failed: native click intercepted and JS fallback also failed.", e);
                }
            }

            // If no JS available, rethrow original exception
            throw new OpenQA.Selenium.WebDriverException("Element click intercepted and no JavaScript executor available for fallback.");
        }

        private IJavaScriptExecutor? GetJavaScriptExecutor()
        {
            if (_driver is IJavaScriptExecutor js) return js;
            if (_driver is IWrapsDriver wraps && wraps.WrappedDriver is IJavaScriptExecutor rawJs) return rawJs;
            return null;
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
        public string GetProperty(string propertyName) => _inner.GetDomProperty(propertyName);
        public string GetDomAttribute(string attributeName) => _inner.GetDomAttribute(attributeName);
        public string GetDomProperty(string propertyName) => _inner.GetDomProperty(propertyName);
        public ISearchContext GetShadowRoot() => _inner.GetShadowRoot();
        public void SendKeys(string text) => _inner.SendKeys(text);
        public void Submit() => _inner.Submit();
    }
}
