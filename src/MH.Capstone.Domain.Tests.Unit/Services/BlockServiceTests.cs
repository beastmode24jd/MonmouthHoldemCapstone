using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using Moq;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class BlockServiceTests
{
    private Mock<IRepository<UserBlock, ApplicationDbContext>> _blockRepoMock = null!;
    private IBlockService _blockService = null!;

    private Guid _alexId;
    private Guid _lilyId;

    [SetUp]
    public void Setup()
    {
        _blockRepoMock = new Mock<IRepository<UserBlock, ApplicationDbContext>>();
        _alexId = Guid.NewGuid();
        _lilyId = Guid.NewGuid();
        _blockService = new BlockService(_blockRepoMock.Object);
    }

    private void SetExistingBlocks(params UserBlock[] blocks)
    {
        _blockRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<UserBlock, bool>>>()))
            .ReturnsAsync((Expression<Func<UserBlock, bool>> pred) => blocks.AsQueryable().Where(pred));
    }

    [Test]
    public async Task BlockAsync_NewPair_PersistsRow()
    {
        SetExistingBlocks();

        await _blockService.BlockAsync(_alexId, _lilyId);

        _blockRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<UserBlock>(b =>
            b.BlockerIdentityId == _alexId.ToString() &&
            b.BlockedIdentityId == _lilyId.ToString())), Times.Once);
    }

    [Test]
    public async Task BlockAsync_AlreadyBlocked_IsNoOp()
    {
        SetExistingBlocks(new UserBlock(_alexId, _lilyId));

        await _blockService.BlockAsync(_alexId, _lilyId);

        _blockRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<UserBlock>()), Times.Never);
    }

    [Test]
    public void BlockAsync_BlockSelf_Throws()
    {
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _blockService.BlockAsync(_alexId, _alexId));
    }

    [Test]
    public async Task UnblockAsync_ExistingBlock_DeletesRow()
    {
        var existing = new UserBlock(_alexId, _lilyId);
        SetExistingBlocks(existing);

        await _blockService.UnblockAsync(_alexId, _lilyId);

        _blockRepoMock.Verify(r => r.DeleteAsync(existing), Times.Once);
    }

    [Test]
    public async Task UnblockAsync_NotBlocked_IsNoOp()
    {
        SetExistingBlocks();

        await _blockService.UnblockAsync(_alexId, _lilyId);

        _blockRepoMock.Verify(r => r.DeleteAsync(It.IsAny<UserBlock>()), Times.Never);
    }

    [Test]
    public async Task IsBlockedAsync_BlockExists_ReturnsTrue()
    {
        SetExistingBlocks(new UserBlock(_alexId, _lilyId));

        var result = await _blockService.IsBlockedAsync(_alexId, _lilyId);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsBlockedAsync_NoBlock_ReturnsFalse()
    {
        SetExistingBlocks();

        var result = await _blockService.IsBlockedAsync(_alexId, _lilyId);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetBlockedUserIdsAsync_ReturnsAllBlockedGuids()
    {
        var third = Guid.NewGuid();
        SetExistingBlocks(
            new UserBlock(_alexId, _lilyId),
            new UserBlock(_alexId, third),
            new UserBlock(_lilyId, _alexId)); // not Alex's blocks

        var result = (await _blockService.GetBlockedUserIdsAsync(_alexId)).ToList();

        Assert.That(result, Is.EquivalentTo(new[] { _lilyId, third }));
    }
}
