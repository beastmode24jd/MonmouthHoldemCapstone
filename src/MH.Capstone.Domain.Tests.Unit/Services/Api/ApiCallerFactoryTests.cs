using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MH.Capstone.Domain.Tests.Unit.Services.Api
{

    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public class ApiCallerFactoryTests
    {
        #region TestingOverhead

        private Mock<IHttpClientFactory> _httpClientFactoryMock;

        [SetUp]
        public void SetUp()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        }

        private ApiCallerFactory<ApiConfigurationValuesFake> CreateSut() =>
            new ApiCallerFactory<ApiConfigurationValuesFake>(
                NullLogger<IApiCallerFactory<ApiConfigurationValuesFake>>.Instance,
                NullLogger<IApiCaller<ApiConfigurationValuesFake>>.Instance,
                _httpClientFactoryMock.Object, ApiConfigurationValuesFake.Instance);

        private void AsseryAllMockVarifySetups()
        {
            _httpClientFactoryMock.VerifyAll();
            _httpClientFactoryMock.VerifyNoOtherCalls();
        }

        #endregion

        #region CreateApiCaller

        [Test]
        public void CreateApiCaller_ReturnsExternalApiCaller()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            var result = sut.CreateApiCaller();

            // Assert
            Assert.That(result, Is.InstanceOf<ExternalApiCaller<ApiConfigurationValuesFake>>());
            AsseryAllMockVarifySetups();
        }

        #endregion
    }
}