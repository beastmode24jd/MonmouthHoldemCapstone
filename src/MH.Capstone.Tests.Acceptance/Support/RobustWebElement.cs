using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Support
{
    public class RobustWebElement : IWebElement, IWrapsElement
    {
        private readonly IWebElement _inner;
        private readonly IWebDriver _driver;

        public RobustWebElement(IWebElement inner, IWebDriver driver)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public IWebElement WrappedElement => _inner;

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
            string Describe()
            {
                try
                {
                    var id = _inner.GetAttribute("id");
                    var cls = _inner.GetAttribute("class");
                    return $"<{_inner.TagName} id='{id}' class='{cls}'>";
                }
                catch { return _inner.TagName; }
            }

            try
            {
                _inner.Click();
                return;
            }
            catch (OpenQA.Selenium.ElementClickInterceptedException ex)
            {
                Console.WriteLine($"RobustWebElement.Click: native click threw {ex.GetType().Name}: {ex.Message}. Element: {Describe()}");
                // continue to fallback
            }
            catch (OpenQA.Selenium.ElementNotInteractableException ex)
            {
                Console.WriteLine($"RobustWebElement.Click: native click threw {ex.GetType().Name}: {ex.Message}. Element: {Describe()}");
                // continue to fallback
            }
            catch (OpenQA.Selenium.WebDriverException ex) when (ex.Message?.Contains("element click intercepted", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.WriteLine($"RobustWebElement.Click: native click threw WebDriverException: {ex.Message}. Element: {Describe()}");
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
                            try
                            {
                                _inner.Click();
                                return;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"RobustWebElement.Click: retry native click failed: {ex.GetType().Name}: {ex.Message}. Element: {Describe()}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"RobustWebElement.Click: checking displayed/enabled threw: {ex.GetType().Name}: {ex.Message}");
                    }
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RobustWebElement.Click: wait loop failed: {ex.GetType().Name}: {ex.Message}");
            }

            // Final fallback: JavaScript click
            var jsFallback = GetJavaScriptExecutor();
            if (jsFallback != null)
            {
                try
                {
                    Console.WriteLine($"RobustWebElement.Click: attempting JavaScript click for Element: {Describe()}");
                    jsFallback.ExecuteScript("arguments[0].click();", _inner);
                    Console.WriteLine($"RobustWebElement.Click: JavaScript click succeeded for Element: {Describe()}");
                    return;
                }
                catch (Exception e)
                {
                    var msg = $"Robust click failed: native click retries failed and JS fallback also failed. Element: {Describe()}. JS error: {e.GetType().Name}: {e.Message}";
                    throw new OpenQA.Selenium.WebDriverException(msg, e);
                }
            }

            // If no JS available, rethrow a descriptive exception
            throw new OpenQA.Selenium.WebDriverException($"Element click intercepted/not interactable and no JavaScript executor available for fallback. Element: {Describe()}");
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

        public string GetAttribute(string attributeName) => _inner.GetAttribute(attributeName)!;
        public string GetCssValue(string propertyName) => _inner.GetCssValue(propertyName);
        public string GetProperty(string propertyName) => _inner.GetDomProperty(propertyName)!;
        public string GetDomAttribute(string attributeName) => _inner.GetDomAttribute(attributeName)!;
        public string GetDomProperty(string propertyName) => _inner.GetDomProperty(propertyName)!;
        public ISearchContext GetShadowRoot() => _inner.GetShadowRoot();
        public void SendKeys(string text)
        {
            // Determine whether this is a file input — file inputs cannot be set via JS for security reasons.
            bool isFileInput = false;
            try
            {
                if (string.Equals(_inner.TagName, "input", StringComparison.OrdinalIgnoreCase))
                {
                    var t = _inner.GetAttribute("type") ?? string.Empty;
                    isFileInput = string.Equals(t, "file", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { /* ignore */ }

            // Try native SendKeys first. For file inputs that are hidden, attempt to make them visible
            // before calling native SendKeys (JS can change styling but cannot set file contents).
            try
            {
                var js = GetJavaScriptExecutor();
                if (isFileInput && ! _inner.Displayed && js != null)
                {
                    try
                    {
                        js.ExecuteScript("arguments[0].style.display='block'; arguments[0].style.visibility='visible'; arguments[0].style.opacity=1; arguments[0].style.position='relative'; arguments[0].style.pointerEvents='auto';", _inner);
                    }
                    catch { }
                }

                _inner.SendKeys(text);
                return;
            }
            catch (OpenQA.Selenium.ElementNotInteractableException)
            {
                // continue to fallback
            }
            catch (OpenQA.Selenium.WebDriverException)
            {
                // continue to fallback
            }

            var jsFallback = GetJavaScriptExecutor();
            if (jsFallback != null)
            {
                if (isFileInput)
                {
                    // Cannot set file inputs via JS — provide a clearer error message.
                    throw new OpenQA.Selenium.WebDriverException("Robust SendKeys failed: native SendKeys failed for file input and JS cannot set file inputs.");
                }

                try
                {
                    // If SendKeys didn't set the value (or threw), set via JS and dispatch events so client-side listeners run.
                    jsFallback.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', {bubbles:true})); arguments[0].dispatchEvent(new Event('change', {bubbles:true}));", _inner, text);
                    return;
                }
                catch (Exception ex)
                {
                    throw new OpenQA.Selenium.WebDriverException("Robust SendKeys failed: native SendKeys failed and JS fallback also failed.", ex);
                }
            }

            throw new OpenQA.Selenium.WebDriverException("SendKeys failed and no JavaScript executor available for fallback.");
        }
        public void Submit() => _inner.Submit();
    }
}
