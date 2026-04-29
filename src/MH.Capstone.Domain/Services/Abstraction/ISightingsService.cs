using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface ISightingsService
    {
        // Returns the points earned for the sighting
        Task<int> CreateSightingAsync(Sighting entity, string ianaTimeZoneId = "America/Los_Angeles");
	Task<IEnumerable<Sighting>> GetSightingsInBoundsAsync(decimal minLat, decimal maxLat, decimal minLng, decimal maxLng);

        bool ValidateImage(IFormFile? imageBuffer);

        #region CSP-145: Sighting Gallery Feature

        // Returns all sightings for the specified user
        Task<IEnumerable<Sighting>> GetUserSightingsAsync(Guid userId);

        #endregion

        #region CSP-96: Community Gallery

        // Returns all sightings from all users, with User navigation property loaded, ordered by most recent
        Task<IEnumerable<Sighting>> GetAllSightingsAsync();

        #endregion

        #region CSP-142: Personal Anidex Collection

        /// <summary>
        /// Returns the authenticated user's personal Anidex — one entry per unique SpeciesName
        /// across their own sightings, with global-derived rarity and per-user discovery count.
        /// Empty collection if the user has no sightings.
        /// </summary>
        Task<IEnumerable<AnidexEntry>> GetUserAnidexAsync(Guid userId);

        #endregion
    }
}
