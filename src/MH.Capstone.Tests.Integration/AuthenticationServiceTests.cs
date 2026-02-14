using MH.Capstone.Domain.DataAccess.Contexts;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MH.Capstone.Tests.Integration;

[TestFixture]
public class AuthenticationServiceTests
{
    private ServiceProvider _serviceProvider;
    private AuthDbContext _context;
    private UserManager<ApplicationUser> _userManager;
    private SignInManager<ApplicationUser> _signInManager;
    private IAuthenticationService? _authService;
    [SetUp]
    public async Task Setup()
    {
        // Create in-memory database for testing
        var services = new ServiceCollection();
        
        services.AddDbContext<AuthDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        
        // Add Identity services
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();
        
        // TODO: Register real AuthenticationService here when we create it
        // services.AddScoped<IAuthenticationService, AuthenticationService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AuthDbContext>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _signInManager = _serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        
        await _context.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        _userManager?.Dispose();
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Test]
    public async Task RegisterUserAsync_WithValidData_CreatesUserInDatabase()
    {
        // This test will FAIL initially because we haven't created the real service yet
        // That's the RED step in Red-Green-Refactor!
        
        Assert.Fail("Test not implemented yet - waiting for real AuthenticationService");
    }
}