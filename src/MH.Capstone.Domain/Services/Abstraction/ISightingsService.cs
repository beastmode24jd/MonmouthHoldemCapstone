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

        #region CSP-199: Paginated Community Gallery

        /// <summary>
        /// Returns a single page of community sightings, newest first, plus the paging
        /// metadata the gallery UI needs. Only this page's rows (and their images) are
        /// fetched from the database, keeping the gallery fast regardless of total volume.
        /// <paramref name="page"/> is 1-based and clamped to a minimum of 1.
        /// </summary>
        Task<PagedResult<Sighting>> GetSightingsPageAsync(int page, int pageSize);

        #endregion

        #region CSP-142: Personal Anidex Collection

        /// <summary>
        /// Returns the authenticated user's personal Anidex — one entry per unique SpeciesName
        /// across their own sightings, with global-derived rarity and per-user discovery count.
        /// Empty collection if the user has no sightings.
        /// </summary>
        Task<IEnumerable<AnidexEntry>> GetUserAnidexAsync(Guid userId);

        #endregion

        #region CSP-172: Sighting Details Page

        /// <summary>
        /// Returns the single sighting with the given Id, or <c>null</c> if no such sighting exists.
        /// The <see cref="Sighting.User"/> navigation property is hydrated via the application's
        /// lazy-loading proxies on first access by the caller.
        /// </summary>
        Task<Sighting?> GetSightingByIdAsync(Guid sightingId);

        #endregion

        #region CSP-37: Edit Sighting

        /// <summary>
        /// Updates the <see cref="Sighting.Description"/> and <see cref="Sighting.SpeciesName"/>
        /// of the sighting owned by <paramref name="userId"/>. Scoring (point value, rarity,
        /// multiplier), GPS coordinates, timestamp, and photo are intentionally left untouched —
        /// the factual record and leaderboard standings are frozen at submission time.
        /// Returns the updated sighting, or <c>null</c> if no sighting with that id exists.
        /// Throws <see cref="UnauthorizedAccessException"/> if the sighting exists but is not
        /// owned by <paramref name="userId"/>.
        /// </summary>
        Task<Sighting?> UpdateSightingAsync(Guid sightingId, Guid userId, string description, string speciesName);

        #endregion
    }
}
