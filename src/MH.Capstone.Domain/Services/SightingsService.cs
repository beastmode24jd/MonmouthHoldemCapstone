using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services
{
    public interface ISightingsService
    {
        Task CreateSightingAsync();

        Task<Sighting> GetSightingByIdAsync(Guid id);
    }

    public class SightingsService : ISightingsService
    {
        public async Task CreateSightingAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<Sighting> GetSightingByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
