using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Support;

public class RobustWebDriver : IWebDriver, IJavaScriptExecutor, IWrapsDriver
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

    // IJavaScriptExecutor
    public object? ExecuteScript(string script, params object?[] args)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(script);
            if (script.Contains("document.readyState") || script.Contains("emailField") || script.Contains("passwordField") || script.Contains("submitBtn"))
            {
                Console.WriteLine($"[RobustWebDriver] Executing diagnostic script: {script}");
            }

            var realArgs = UnwrapArgs(args);

            if (_raw is IJavaScriptExecutor js) return js.ExecuteScript(script, realArgs);
            throw new NotSupportedException("Underlying driver does not support JavaScript execution.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RobustWebDriver] ExecuteScript failed: {ex.GetType().Name} {ex.Message}");
            throw;
        }
    }

    public object? ExecuteScript(PinnedScript script, params object?[] args)
    {
        var realArgs = UnwrapArgs(args);
        if (_raw is IJavaScriptExecutor js) return js.ExecuteScript(script, realArgs);
        throw new NotSupportedException("Underlying driver does not support JavaScript execution.");
    }

    public object? ExecuteAsyncScript(string script, params object?[] args)
    {
        try
        {
            var realArgs = UnwrapArgs(args);
            if (_raw is IJavaScriptExecutor js) return js.ExecuteAsyncScript(script, realArgs);
            throw new NotSupportedException("Underlying driver does not support JavaScript execution.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RobustWebDriver] ExecuteAsyncScript failed: {ex.GetType().Name} {ex.Message}");
            throw;
        }
    }

    private static object?[] UnwrapArgs(object?[] args)
    {
        if (args == null! || args.Length == 0) return args!;
        var real = new object?[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is RobustWebElement r) real[i] = r.WrappedElement;
            else real[i] = a;
        }
        return real;
    }

    // IWrapsDriver
    public IWebDriver WrappedDriver => _raw;
}
