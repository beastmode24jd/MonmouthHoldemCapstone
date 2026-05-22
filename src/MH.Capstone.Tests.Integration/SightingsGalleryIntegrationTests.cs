using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MH.Capstone.Tests.Integration
{
    [TestFixture]
    public class SightingsGalleryIntegrationTests
    {
        private WebApplicationFactory<Program> _factory;
        private HttpClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Test]
        public async Task Gallery_ReturnsOk()
        {
            var response = await _client.GetAsync("/Sighting/Gallery");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task Gallery_WithPageQueryParam_ReturnsOk()
        {
            // CSP-199: the paginated gallery must accept a ?page=N query param and
            // serve the request without error.
            var response = await _client.GetAsync("/Sighting/Gallery?page=2");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }
}
