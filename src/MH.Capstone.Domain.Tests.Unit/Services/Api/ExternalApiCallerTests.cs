using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using MH.Capstone.Domain.ApiContracts;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging.Abstractions;
using MH.Capstone.Domain.Services.Api;

namespace MH.Capstone.Domain.Tests.Unit.Services.Api
{
    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public class ExternalApiCallerTests
    {
        #region TestingOverhead

        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<IApiConfigurationValues> _apiConfigMock;
        private MockHttpMessageHandler _mockHttp;
        private static readonly ApiConfigurationValuesFake _configurationValuesFake 
            = ApiConfigurationValuesFake.Instance;

        [SetUp]
        public void SetUp()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _apiConfigMock = new Mock<IApiConfigurationValues>();
            _mockHttp = new MockHttpMessageHandler();
        }

        [TearDown]
        public void TearDown()
        {
            _mockHttp.Dispose();
        }

        private ExternalApiCaller<ApiConfigurationValuesFake> CreateSut() =>
            new ExternalApiCaller<ApiConfigurationValuesFake>(
                NullLogger<IApiCaller<ApiConfigurationValuesFake>>.Instance,
                _httpClientFactoryMock.Object, _configurationValuesFake);

        private void AsseryAllMockVarifySetups()
        {
            _httpClientFactoryMock.VerifyAll();
            _httpClientFactoryMock.VerifyNoOtherCalls();

            _mockHttp.VerifyNoOutstandingExpectation();
        }

        #endregion

        #region GetAsyncTests

        [Test]
        public async Task GetAsync_NoErrors_ReturnsProperlyDeserializeReturn()
        {
            // Arrange
            const string expectedUrl = "https://wou.edu";
            int[] expectedResponse = [1,2,3,4,5];
            var expectedResponseJson = JsonConvert.SerializeObject(expectedResponse);

            _mockHttp.Expect(HttpMethod.Get, expectedUrl)
                .Respond("application/json", expectedResponseJson);

            _httpClientFactoryMock.Setup(f => f.CreateClient(
                    _configurationValuesFake.HttpClientKey))
                    .Returns(_mockHttp.ToHttpClient()).Verifiable(Times.Once);

            var sut = CreateSut();

            // Act
            var result = await sut.GetAsync<int[]>(expectedUrl);
            
            // Assert
            Assert.That(result, Is.EqualTo(expectedResponse));
            AsseryAllMockVarifySetups();
        }

        [Test]
        public void GetAsync_CallNotSuccessful_ThrowsHttpRequestException()
        {
            // Arrange
            const string expectedUrl = "https://wou.edu";
            const HttpStatusCode expectedStatusCode = HttpStatusCode.BadRequest;

            _mockHttp.Expect(HttpMethod.Get, expectedUrl).Respond(expectedStatusCode);

            _httpClientFactoryMock.Setup(f => f.CreateClient(
                    _configurationValuesFake.HttpClientKey))
                .Returns(_mockHttp.ToHttpClient()).Verifiable(Times.Once);

            var sut = CreateSut();

            // Act & Assert
            var actual = Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAsync<object>(expectedUrl));
            Assert.That(actual.StatusCode, Is.EqualTo(expectedStatusCode));
            AsseryAllMockVarifySetups();
        }

        [Test]
        public void GetAsync_BadGenericReturnTypePreventsJsonDeserialize_ThrowsJsonException()
        {
            // Arrange
            const string expectedUrl = "https://wou.edu";
            const HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            _mockHttp.Expect(HttpMethod.Get, expectedUrl).Respond(expectedStatusCode);

            _httpClientFactoryMock.Setup(f => f.CreateClient(
                    _configurationValuesFake.HttpClientKey))
                .Returns(_mockHttp.ToHttpClient()).Verifiable(Times.Once);

            var sut = CreateSut();

            // Act & Assert
            Assert.ThrowsAsync<JsonException>(() => sut.GetAsync<Mock>(expectedUrl));
            AsseryAllMockVarifySetups();
        }

        #endregion
    }
}
