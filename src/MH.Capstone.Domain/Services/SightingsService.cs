using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static MH.Capstone.Domain.Tools.DataAnnotationsValidator;

namespace MH.Capstone.Domain.Services
{
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

        public bool ValidateImage(IFormFile? imageBuffer)
        {
            //_logger.LogInformation($"Null? {imageBuffer == null}\n" +
            //                       $"Length: {imageBuffer?.Length}\n" +
            //                       $"Type: {imageBuffer?.ContentType}");

            // Null check - if null, we are not valid
            if (imageBuffer == null)
            {
                return false;
            }

            // Check file size (max 2 MB) but needs to not be 0
            if (imageBuffer.Length is > 2 * 1024 * 1024 or <= 0)
            {
                return false;
            }

            // Check file type (must be an JPG or PNG)
            string[] validImgTypes = ["jpg", "jpeg", "png"];
            if (!validImgTypes.Any(t => imageBuffer.ContentType.Contains($"image/{t}")))
            {
                return false;
            }

            // If we made it here, the image is valid
            return true;
        }
    }
}
