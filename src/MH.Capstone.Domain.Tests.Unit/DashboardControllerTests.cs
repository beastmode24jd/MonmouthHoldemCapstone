using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models.ViewModels;
using MH.Capstone.WebApp.Services;
using System.Security.Claims;
using System.Text;
using MH.Capstone.WebApp.Models;

namespace MH.Capstone.Domain.Tests.Unit;

[TestFixture]
public class DashboardControllerTests
{
    // Mocks and method access
    private Mock<IAuthenticationService> _mockAuthService;
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
        _controller = new DashboardController(_mockLogger.Object, _mockService.Object, _mockAuthService.Object);

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

        // Act
        await _controller.UploadProfileImage(fileMock.Object);

        // Assert
        // Verify that the Auth Service was told to update the user's profile image
        // in localDB
        _mockAuthService.Verify(s => s.UpdateUserProfileImage(
        TestEmail,
        It.Is<byte[]>(b => b.Length > 0)), 
    Times.Once);
    }

    [Test]
    public void Index_SetsViewBagWithUserImageUrl()
    {
        // Arrange
        var testBytes = Encoding.UTF8.GetBytes("fake-image-data");
        var mockUser = new ApplicationUser { 
            Email = TestEmail, 
            ProfileImage = testBytes
        };
        
        _mockAuthService.Setup(s => s.GetUserByEmail(TestEmail))
                        .Returns(mockUser);

        // Act
        var result = _controller.Index() as ViewResult;

        // Assert
        string expectedBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(testBytes)}";
        Assert.That(_controller.ViewBag.ProfileImageUrl, Is.EqualTo(expectedBase64));
    }

    /*
    [Test]
    public async Task Upload_NotSuccessful_DoesNotRunService()
    {
        // Arrange and Act
        var result = await _controller.UploadProfileImage(null);

        // Assert
        var redirect = result as RedirectToActionResult;
        Assert.That(redirect?.ActionName, Is.EqualTo("Index"));
        _mockService.Verify(s => s.UploadImageAsync(It.IsAny<IFormFile>()), Times.Never);
    }

*/

/*
    [Test]
    public async Task SaveBio_Successful_UpdatesProfileAttributes()
    {
        // Arrange
        // Set up Dashboard view with dummy bio text variable

        // Act
        // Run through the Account Controller SaveBio method,
        //      with the bio text variable

        // Assert
        // Check that the bio text matches the aspect field.
        
    }

    [Test]
    public async Task SaveBio_EmptyString_DisplaysErrorMessage()
    {
        // Arrange
        // Set up Dashboard view with dummy bio text variable as empty string

        // Act
        // Run through the Account Controller SaveBio method,
        //      with empty string

        // Assert
        // Check that an error message is displayed to the user,
        //  and that the bio field in the Model remains blank.
        
    }

*/

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