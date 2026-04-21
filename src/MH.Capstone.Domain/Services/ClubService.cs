using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services
{
    public class ClubService : IClubService
    {
        /* IMPORTANT FUTURE CLUB SERVICE FILE NOTE!!!

            "Deleting a user will now throw if they still have club memberships or messages.
            Your service layer will need to clean those up before deleting a user."

        */
        private readonly IRepository<Club, ApplicationDbContext> _clubRepo;

        public ClubService(IRepository<Club, ApplicationDbContext> clubRepo)
        {
            _clubRepo = clubRepo;
        }

        public Task<IEnumerable<Club>> GetPublicClubsAsync()
        {
            throw new NotImplementedException();
        }
    }
}
