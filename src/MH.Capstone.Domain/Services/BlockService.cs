using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services
{
    public class BlockService : IBlockService
    {
        private readonly IRepository<UserBlock, ApplicationDbContext> _blockRepo;

        public BlockService(IRepository<UserBlock, ApplicationDbContext> blockRepo)
        {
            _blockRepo = blockRepo;
        }

        public async Task BlockAsync(Guid blockerId, Guid blockedId)
        {
            if (blockerId == blockedId)
                throw new InvalidOperationException("A user cannot block themselves.");

            var blockerKey = blockerId.ToString();
            var blockedKey = blockedId.ToString();

            var existing = (await _blockRepo.GetAllAsync(
                b => b.BlockerIdentityId == blockerKey && b.BlockedIdentityId == blockedKey))
                .FirstOrDefault();

            if (existing != null) return;

            await _blockRepo.AddOrUpdateAsync(new UserBlock(blockerId, blockedId));
        }

        public async Task UnblockAsync(Guid blockerId, Guid blockedId)
        {
            var blockerKey = blockerId.ToString();
            var blockedKey = blockedId.ToString();

            var existing = (await _blockRepo.GetAllAsync(
                b => b.BlockerIdentityId == blockerKey && b.BlockedIdentityId == blockedKey))
                .FirstOrDefault();

            if (existing == null) return;

            await _blockRepo.DeleteAsync(existing);
        }

        public async Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId)
        {
            var blockerKey = blockerId.ToString();
            var blockedKey = blockedId.ToString();

            return (await _blockRepo.GetAllAsync(
                b => b.BlockerIdentityId == blockerKey && b.BlockedIdentityId == blockedKey))
                .Any();
        }

        public async Task<IEnumerable<Guid>> GetBlockedUserIdsAsync(Guid blockerId)
        {
            var blockerKey = blockerId.ToString();
            var rows = await _blockRepo.GetAllAsync(b => b.BlockerIdentityId == blockerKey);
            return rows.AsEnumerable().Select(b => b.BlockedId).ToList();
        }
    }
}
