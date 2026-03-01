using System.Security.Claims;
using System.Text;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
public class DashboardControllerTests
{
    // Mocks and method access
    private Mock<IAuthenticationService> _mockAuthService;
    private Mock<IUserService> _mockUserService;
    private Mock<IProfileImageService> _mockService;
    private Mock<ILogger<DashboardController>> _mockLogger;
    private DashboardController _controller;
    private const string TestEmail = "namesNameington@mail.wou";

    [SetUp]
    public void SetUp()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockService = new Mock<IProfileImageService>();
        _mockLogger = new Mock<ILogger<DashboardController>>();
        _mockUserService = new Mock<IUserService>();
        _controller = new DashboardController(_mockLogger.Object, _mockService.Object, 
            _mockAuthService.Object, _mockUserService.Object);

        // Mock the user, so the display name isn't null while testing
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, TestEmail),
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };


    }

    [TearDown]
    public void TearDown()
    {
        // Dispose of controller to satisfy line 20 being unhappy
        _controller?.Dispose();
    }

    [Test]
    public async Task UploadProfileImage_Successful_ByteArrayToService()
    {
        // Arrange
        var fileMock = CreateMockFile("test.jpg", "image/jpeg", "content");
        var dummyBytes = new byte[] { 0x20, 0x21, 0x22 }; // Creates dummy data

        // Tells service to return dummy bytes
        _mockService.Setup(s => s.ConvertToBytesAsync(It.IsAny<IFormFile>()))
                    .ReturnsAsync(dummyBytes);

        // Act
        await _controller.UploadProfileImage(fileMock.Object);

        // Assert
        // Verify that the Auth Service was told to update the user's profile image
        // in localDB
        _mockUserService.Verify(s => s.UpdateUserProfileImageAsync(
        TestEmail,
        It.Is<byte[]>(b => b.Length > 0),
        fileMock.Object.ContentType), 
    Times.Once);
    }

    // Helper method, for mocking:
    private Mock<IFormFile> CreateMockFile(string fileName, string contentType, string content)
    {
        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns(contentType);
        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream stream, CancellationToken token) => ms.CopyToAsync(stream, token));

        return fileMock;
    }
}