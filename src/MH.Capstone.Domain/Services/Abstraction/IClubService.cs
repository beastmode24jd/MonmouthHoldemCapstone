using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IClubService
    {
        Task<IEnumerable<Club>> GetPublicClubsAsync();
    }
}
