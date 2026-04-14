using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Helpers
{
    public static class WebDriverExtensions
    {
        /// <summary>
        /// Generic explicit wait. Retries the provided condition until it returns a non-null/non-false value or times out.
        /// Ignores NoSuchElementException and StaleElementReferenceException while waiting.
        /// </summary>
        public static T WaitUntil<T>(this IWebDriver driver, Func<IWebDriver, T> condition, 
            TimeSpan? timeout = null, TimeSpan? pollInterval = null)
        {
            var wait = new DefaultWait<IWebDriver>(driver)
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(10),
                PollingInterval = pollInterval ?? TimeSpan.FromMilliseconds(250)
            };

            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
            return wait.Until(condition);
        }

        /// <summary>
        /// Waits until all provided conditions return a "truthy" value or times out.
        /// A value is considered truthy if it is not null. If the value is a boolean it must be true.
        /// Returns an array containing the results of each condition in the same order as provided.
        /// </summary>
        public static T[]? WaitUntilAll<T>(this IWebDriver driver, IEnumerable<Func<IWebDriver, T>> conditions,
            TimeSpan? timeout = null, TimeSpan? pollInterval = null)
        {
            var condList = conditions?.ToList() ?? new List<Func<IWebDriver, T>>();
            return driver.WaitUntil(d =>
            {
                var results = new T[condList.Count];
                for (var i = 0; i < condList.Count; i++)
                {
                    var res = condList[i](d);
                    results[i] = res;
                    if (!IsTruthy(res))
                        return null; // keep waiting
                }

                return results;
            }, timeout, pollInterval);
        }

        /// <summary>
        /// Waits until any of the provided conditions returns a "truthy" value or times out.
        /// A value is considered truthy if it is not null. If the value is a boolean it must be true.
        /// Returns a Tuple of (index, result) where index is the zero-based index of the satisfied condition.
        /// </summary>
        public static Tuple<int, T>? WaitUntilAny<T>(this IWebDriver driver, IEnumerable<Func<IWebDriver, T>> conditions,
            TimeSpan? timeout = null, TimeSpan? pollInterval = null)
        {
            var condList = conditions?.ToList() ?? new List<Func<IWebDriver, T>>();
            return driver.WaitUntil(d =>
            {
                for (var i = 0; i < condList.Count; i++)
                {
                    var res = condList[i](d);
                    if (IsTruthy(res))
                        return Tuple.Create(i, res);
                }
                return null; // keep waiting
            }, timeout, pollInterval);
        }

        /// <summary>
        /// Helper used to determine whether a condition result should be considered satisfied.
        /// </summary>
        private static bool IsTruthy(object? value)
        {
            return value switch
            {
                null => false,
                bool b => b,
                _ => true // default for non-boolean values is to consider them truthy if not null
            };
        }

        /// <summary>
        /// Waits for an element to exist (returns the element when found).
        /// </summary>
        public static IWebElement WaitForElement(this IWebDriver driver, By by, TimeSpan? timeout = null)
        {
            return driver.WaitUntil(d => d.FindElement(by), timeout);
        }

        /// <summary>
        /// Waits until an element exists and is displayed.
        /// </summary>
        public static bool WaitForElementVisible(this IWebDriver driver, By by, TimeSpan? timeout = null)
        {
            return driver.WaitUntil(d =>
            {
                var elems = d.FindElements(by);
                return elems.Count > 0 && elems[0].Displayed;
            }, timeout);
        }

        /// <summary>
        /// Waits until the document.readyState === 'complete'.
        /// Useful for full page navigations.
        /// </summary>
        public static bool WaitForDocumentReady(this IWebDriver driver, TimeSpan? timeout = null)
        {
            return driver.WaitUntil(d =>
            {
                try
                {
                    var script = "return document.readyState";
                    var ready = ((IJavaScriptExecutor)d).ExecuteScript(script)?.ToString();
                    return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }, timeout);
        }
    }
}