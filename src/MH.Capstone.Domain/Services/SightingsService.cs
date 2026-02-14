using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services
{
    public interface ISightingsService
    {
        Task CreateSightingAsync(Sighting entity);

        Task<Sighting> GetSightingByIdAsync(Guid id);
    }

    public class SightingsService : ISightingsService
    {
        private ILogger<SightingsService> _logger;
        private IRepository<Sighting> _sightingsRepo;

        public SightingsService(ILogger<SightingsService> logger, IRepository<Sighting> sightingsRepo)
        {
            _logger = logger;
            _sightingsRepo = sightingsRepo;
        }

        public async Task CreateSightingAsync(Sighting entity)
        {
            throw new NotImplementedException();
        }

        public async Task<Sighting> GetSightingByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
