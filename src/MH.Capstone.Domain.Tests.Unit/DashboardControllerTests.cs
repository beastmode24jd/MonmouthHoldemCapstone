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

namespace MH.Capstone.Domain.Tests.Unit;

[TestFixture]
public class DashboardControllerTests
{
    // Mocks and method access
    private Mock<IProfileImageService> _mockService;
    private Mock<ILogger<DashboardController>> _mockLogger;
    private DashboardController _controller;

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<IProfileImageService>();
        _mockLogger = new Mock<ILogger<DashboardController>>();
        _controller = new DashboardController(_mockLogger.Object, _mockService.Object);

        // Mock the user, so the display name isn't null while testing
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, "namesMcNameington@mail.wou"),
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() {User = user}
        };


    }

    [Test]
    public async Task UploadProfileImage_Successful_UploadsProfileImage()
    {
        // Arrange
        var fileMock = CreateMockFile("test.jpg", "image/jpeg", "fake content");
        _mockService.Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("uploads/profiles/test.jpeg");

        // Act
        var result = await _controller.UploadProfileImage(fileMock.Object);

        // Assert
        var redirect = result as RedirectToActionResult;
        Assert.That(redirect?.ActionName, Is.EqualTo("Index"));
        _mockService.Verify(s => s.UploadImageAsync(It.IsAny<IFormFile>()), Times.Once);

    }

    [Test]
    public async Task Upload_NotSuccessful_DoesNotRunService()
    {
        // Arrange
        var mockService = new Mock<IImageService>();
        mockService.Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("https://fake.blob.core/image.jpg");

        _controller = new AccountController(mockService.Object, _logger);

        // Act
        await _controller.Upload(1, someFakeFile);
        // "Some fake file" should be over the set file size limit.

        // Assert
        var profile = _context.Profiles.NotFound(1);
    }

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
}