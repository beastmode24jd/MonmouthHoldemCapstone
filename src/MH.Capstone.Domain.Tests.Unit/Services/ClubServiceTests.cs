using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.Services.Abstraction;
using System.Text;
using Moq;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class ClubServiceTests
{
    private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock;
    private Mock<IRepository<Badge, ApplicationDbContext>> _badgeRepoMock;
    private Mock<IRepository<UserBadge, ApplicationDbContext>> _userBadgeRepoMock;
    private Mock<INotificationService> _notificationServiceMock;
    private IBadgeService _clubService;
    

    [SetUp]
    public void Setup()
    {
        // Add in the Mocked Repositories
        _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();
        _badgeRepoMock = new Mock<IRepository<Badge, ApplicationDbContext>>();
        _userBadgeRepoMock = new Mock<IRepository<UserBadge, ApplicationDbContext>>();
        _notificationServiceMock = new Mock<INotificationService>();

    }
}