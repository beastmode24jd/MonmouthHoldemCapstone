using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services.Notifications
{
    public class LiveNotificationPreferenceService : ILiveNotificationPreferenceService
    {
        private readonly IRepository<LiveNotificationPreference, ApplicationDbContext> _repo;

        public LiveNotificationPreferenceService(IRepository<LiveNotificationPreference, ApplicationDbContext> repo)
        {
            _repo = repo;
        }

        public async Task<bool> IsEnabledAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return true;

            var stored = (await _repo.GetAllAsync(p => p.UserId == userId)).FirstOrDefault();
            return stored?.LiveUpdatesEnabled ?? true;
        }

        public async Task SetEnabledAsync(string userId, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var existing = (await _repo.GetAllAsync(p => p.UserId == userId)).FirstOrDefault();
            if (existing != null)
            {
                existing.LiveUpdatesEnabled = enabled;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                await _repo.AddOrUpdateAsync(existing);
            }
            else
            {
                await _repo.AddOrUpdateAsync(new LiveNotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    LiveUpdatesEnabled = enabled,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
        }
    }
}
