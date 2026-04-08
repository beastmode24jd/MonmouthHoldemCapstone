using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects
{
    [ExcludeFromCodeCoverage]
    public class SightingsUploadPageObject
    {
        public IWebElement LatInput => _latInput.Value;
        private readonly Lazy<IWebElement> _latInput;

        public IWebElement LongInput => _longInput.Value;
        private readonly Lazy<IWebElement> _longInput;

        public IWebElement TimeInput => _timeInput.Value;
        private readonly Lazy<IWebElement> _timeInput;

        public IWebElement DescInput => _descInput.Value;
        private readonly Lazy<IWebElement> _descInput;

        public IWebElement ImageUploadBtn => _imageUploadBtn.Value;
        private readonly Lazy<IWebElement> _imageUploadBtn;
        
        public IWebElement SubmitBtn => _submitBtn.Value;
        private readonly Lazy<IWebElement> _submitBtn;

        public SightingsUploadPageObject(IWebDriver webDriver)
        {
            // TODO - Put this URL in a config file or something similar so that it can be run against different environments
            const string url = "https://localhost:7147/Sighting/Create";

            if (!string.Equals(webDriver.Url, url, StringComparison.InvariantCultureIgnoreCase))
            {
                webDriver.Navigate().GoToUrl(url);
            }

            _latInput = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("Latitude")));
            _longInput = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("Longitude")));
            _timeInput = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("Timestamp")));
            _descInput = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("Description")));
            _imageUploadBtn = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("UploadedImage")));
            _submitBtn = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("SubmitBtn")));
        }
    }
}