using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class ClubsLandingPageObject
{
    // Filter bar
    public IWebElement FilterMineBtn  => _filterMineBtn.Value;

    // Create-Club modal inputs (present in DOM even when modal is hidden)
    public IWebElement ModalClubNameInput => _modalClubNameInput.Value;
    public IWebElement ModalDescInput     => _modalDescInput.Value;
    public IWebElement ModalConfirmBtn    => _modalConfirmBtn.Value;

    private readonly Lazy<IWebElement> _filterMineBtn;
    private readonly Lazy<IWebElement> _modalClubNameInput;
    private readonly Lazy<IWebElement> _modalDescInput;
    private readonly Lazy<IWebElement> _modalConfirmBtn;

    public ClubsLandingPageObject(IWebDriver webDriver, string baseUrl)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Clubs";

        // Navigate only if not already on the clubs landing page.
        // (Same guard used by SightingsUploadPageObject so re-constructing the object
        // mid-scenario does not wipe state set by earlier steps.)
        if (!webDriver.Url.StartsWith(url, StringComparison.InvariantCultureIgnoreCase))
            webDriver.Navigate().GoToUrl(url);

        _filterMineBtn     = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("filterMine")));
        _modalClubNameInput = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("modalClubName")));
        _modalDescInput    = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("descInput")));
        _modalConfirmBtn   = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("confirmAuthBtn")));
    }
}
