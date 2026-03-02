using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging.Abstractions;

namespace MH.Capstone.Domain.Tests.Unit.Services
{
    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public class ExternalApiCallerTests
    {
        #region TestingOverhead

        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private MockHttpMessageHandler _mockHttp;

        [SetUp]
        public void SetUp()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _mockHttp = new MockHttpMessageHandler();
        }

        [TearDown]
        public void TearDown()
        {
            _mockHttp.Dispose();
        }

        private ExternalApiCaller CreateSut(string clientName) =>
            new ExternalApiCaller(NullLogger<IApiCaller>.Instance, _httpClientFactoryMock.Object,
                clientName);

        private void AsseryAllMockVarifySetups()
        {
            _httpClientFactoryMock.VerifyAll();
            _httpClientFactoryMock.VerifyNoOtherCalls();
        }

        #endregion

        #region GetAsyncTests

        [Test]
        public async Task GetAsync_NoErrors_CallsHttpClientFactoryWithCorrectClientName()
        {
            // Arrange
            const string clientName = "TestClient";
            const string expectedUrl = "http://example.com";
            int[] expectedResponse = [1,2,3,4,5];
            var expectedResponseJson = JsonConvert.SerializeObject(expectedResponse);

            _mockHttp.When(HttpMethod.Get, expectedUrl)
                .Respond("application/json", expectedResponseJson);

            _httpClientFactoryMock.Setup(f => f.CreateClient(clientName))
                    .Returns(_mockHttp.ToHttpClient()).Verifiable(Times.Once);

            var sut = CreateSut(clientName);

            // Act
            var result = await sut.GetAsync<int[]>(expectedUrl);
            
            // Assert
            Assert.That(result, Is.EqualTo(expectedResponse));
            AsseryAllMockVarifySetups();
        }

        [Test]
        public void GetAsync_ClientNameNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            const string expectedUrl = "http://example.com";
            var expectedException = new InvalidOperationException("Client not found, thus base not set!");

            _mockHttp.When(HttpMethod.Get, expectedUrl)
                .Respond(_ => throw expectedException);

            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(_mockHttp.ToHttpClient()).Verifiable(Times.Once);

            var sut = CreateSut("BadName");

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetAsync<object>(expectedUrl));
            AsseryAllMockVarifySetups();
        }

        [Test]
        public void GetAsync_CallNotSuccessful_ThrowsHttpRequestException()
        {
            // Arrange
            const string clientName = "TestClient";
            const string expectedUrl = "http://example.com";
            const HttpStatusCode expectedStatusCode = HttpStatusCode.BadRequest;

            _mockHttp.When(HttpMethod.Get, expectedUrl).Respond(expectedStatusCode);

            _httpClientFactoryMock.Setup(f => f.CreateClient(clientName))
                .Returns(_mockHttp.ToHttpClient()).Verifiable(Times.Once);

            var sut = CreateSut(clientName);

            // Act & Assert
            var actual = Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAsync<object>(expectedUrl));
            Assert.That(actual.StatusCode, Is.EqualTo(expectedStatusCode));
            AsseryAllMockVarifySetups();
        }

        [Test]
        public void GetAsync_BadGenericTypePreventsJsonDeserialize_ThrowsJsonException()
        {
            // Arrange
            const string clientName = "TestClient";
            const string expectedUrl = "http://example.com";
            const HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            _mockHttp.When(HttpMethod.Get, expectedUrl).Respond(expectedStatusCode);

            _httpClientFactoryMock.Setup(f => f.CreateClient(clientName))
                .Returns(_mockHttp.ToHttpClient()).Verifiable(Times.Once);

            var sut = CreateSut(clientName);

            // Act & Assert
            Assert.ThrowsAsync<JsonException>(() => sut.GetAsync<Mock>(expectedUrl));
            AsseryAllMockVarifySetups();
        }

        #endregion
    }
}
