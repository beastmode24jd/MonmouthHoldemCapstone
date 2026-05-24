using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class EditSightingPageObject
{
    private readonly IWebDriver _webDriver;

    public EditSightingPageObject(IWebDriver webDriver)
    {
        _webDriver = webDriver;
    }

    public IReadOnlyCollection<IWebElement> Forms => _webDriver.FindElements(By.Id("editSightingForm"));
    public IWebElement DescriptionField => _webDriver.FindElement(By.Id("editDescriptionField"));
    public IWebElement SpeciesField => _webDriver.FindElement(By.Id("editSpeciesField"));
    public IWebElement SaveButton => _webDriver.FindElement(By.Id("saveEditBtn"));
    public IWebElement CancelLink => _webDriver.FindElement(By.Id("cancelEditLink"));

    // Any field-level validation message or the validation summary rendered with text.
    public IReadOnlyCollection<IWebElement> ValidationErrors =>
        _webDriver.FindElements(By.CssSelector(".text-danger, .field-validation-error, .validation-summary-errors"));
}
