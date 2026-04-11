using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Services.Abstraction

{
    // Defines the contract for the scoring service with two methods: 
    // CalculatePointsAsync (takes global count, returns points) 
    // and GetGlobalSightingsCountAsync (takes species ID, returns global count).

    public interface IScoringService
    {

        /// <summary>
        /// Points will be awarded for a sighting based on global rarity
        /// using the formula: Points = BasePoints × RarityMultiplier
        /// </summary>
        /// <param name="globalSightingsCount">The number of global sightings for the species.</param>
        /// <returns>The calculated points for the sighting.</returns>
        
        /// Rarity Tiers 
        /// - Mythic: 5.0× (≤5 global sightings)
        /// - Rare: 2.0× (≤50 global sightings)
        /// - Common: 1.0× (>50 global sightings)
        Task<int> CalculatePointsAsync(int globalSightingsCount);

        /// <summary>
        /// Gets the total global count of sightings for a specific species.
        /// This will be used to determine rarity tier.
        /// </summary>
        /// <param name="speciesId">The unique identifier of the species.</param>
        /// <returns>The total global count of sightings for the specified species.</returns>

        Task<int> GetGlobalSightingsCountAsync(int speciesId);

        /// <summary>
        /// Takes the global count of the species, and returns the base point amount, rarity multipler, and name in an implicit tuple of an int, double, and a string.
        /// Currently used for saving extra data to Sightings, and for display in the Gallery.
        /// </summary>
        /// <param name="globalSightingsCount"></param>
        /// <returns>An implicit tuple holding the current base points amount, Rarity multipler, and the Rarity name.</returns>
        Task<(int basePoints, double multipler, string name)> GetRarityMultiplierAndName(int globalSightingsCount);
    }
}