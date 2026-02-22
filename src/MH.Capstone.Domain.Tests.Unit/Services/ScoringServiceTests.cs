using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics.CodeAnalysis;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class ScoringServiceTests
{
    private Mock<IRepository<Sighting, ApplicationDbContext>> _sightingsRepoMock = null!;

    // Remember: Arrange, Act, Assert
    [SetUp]
    public void Setup()
    {
        _sightingsRepoMock = new Mock<IRepository<Sighting, ApplicationDbContext>>();
    }

    private ScoringService CreateSut() =>
        new(NullLogger<ScoringService>.Instance, _sightingsRepoMock.Object);

    // Step 2 - Add test methods here
}
