using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess.Contexts;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Tools;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static MH.Capstone.Domain.Tools.DataAnnotationsValidator;

namespace MH.Capstone.Domain.Services
{
    public interface ISightingsService
    {
        Task CreateSightingAsync(Sighting entity);

        Task<Sighting> GetSightingByIdAsync(Guid id);
    }

    public class SightingsService : ISightingsService
    {
        private readonly ILogger<SightingsService> _logger;
        private readonly IRepository<Sighting, ApplicationDbContext> _sightingsRepo;

        public SightingsService(ILogger<SightingsService> logger, IRepository<Sighting, ApplicationDbContext> sightingsRepo)
        {
            _logger = logger;
            _sightingsRepo = sightingsRepo;
        }

        public async Task CreateSightingAsync(Sighting entity)
        {
            if (!entity.TryValidateEntity(out var fails))
            {
                // There were one or more validation failures. Since this is a service method, we will throw an
                // exception to be handled by the caller and only care about the first failure for logging purposes.
                var firstFail = fails.First();
                throw new ArgumentException($"Sighting entity validation failed. Property {firstFail} invalid.",
                    firstFail);
            }

            try
            {
                await _sightingsRepo.AddOrUpdateAsync(entity);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
            {
                // This is a SQL foreign key violation, which means the UserId provided does not exist in the Users table.
                throw new ArgumentException(
                    $"Sighting entity validation failed. UserId {entity.UserId} does not exist.", nameof(entity.UserId),
                    ex);
            }
        }

        public async Task<Sighting> GetSightingByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
