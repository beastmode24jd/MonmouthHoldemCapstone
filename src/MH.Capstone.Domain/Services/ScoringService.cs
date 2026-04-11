using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services
{
    // The Scoring Service class will implement the Base Points and Rarity scoring Algorithm
    // and calculate points for wildlife sightings based on global rarity

    public class ScoringService : IScoringService
    {
        private readonly ILogger<ScoringService> _logger;
        private readonly IRepository<Sighting, ApplicationDbContext> _sightingsRepo;

        // Set constants for base points and rarity multiplies

        //Base points constant - ALL sightings start with 10 points before rarity multiplier is applied
        private const int BasePoints = 10;

        // Rarity tier Thresholds - these will be used to determine which rarity multiplier to apply based on the global sightings count for the species
        private const int MythicThreshold = 5;
        private const int RareThreshold = 50;

        //Rarity multipliers
        private const double MythicMultiplier = 5.0;
        private const double RareMultiplier = 2.0;
        private const double CommonMultiplier = 1.0;

         public ScoringService(
            ILogger<ScoringService> logger,
            IRepository<Sighting, ApplicationDbContext> sightingsRepo)
        {
            _logger = logger;
            _sightingsRepo = sightingsRepo;
        }

        /// <summary>
        /// CSP-109: Calculate points based on global sighting count
        /// Formula: Points = BasePoints × RarityMultiplier
        /// </summary>
        public async Task<int> CalculatePointsAsync(int globalSightingsCount)
        {
            if (globalSightingsCount < 0)
            {
                throw new ArgumentException("Global sighting count cannot be negative", nameof(globalSightingsCount));
            }

            // Determine rarity multiplier based on global count
            double multiplier = globalSightingsCount switch
            {
                <= MythicThreshold => MythicMultiplier,   // ≤5 sightings: Mythic (5.0×)
                <= RareThreshold => RareMultiplier,       // ≤50 sightings: Rare (2.0×)
                _ => CommonMultiplier                      // >50 sightings: Common (1.0×)
            };

            // Calculate final points
            int points = (int)(BasePoints * multiplier);

            _logger.LogInformation(
                "Calculated {Points} points for sighting (Global Count: {Count}, Multiplier: {Multiplier}×)",
                points, globalSightingsCount, multiplier);

            return await Task.FromResult(points);
        }

        /// <summary>
        /// CSP-108: Get global sighting count for a species
        /// This will be used to determine rarity tier
        /// </summary>
        public async Task<int> GetGlobalSightingsCountAsync(int speciesId)
        {
            if (speciesId <= 0)
            {
                throw new ArgumentException("Species ID must be positive", nameof(speciesId));
            }

            // Once Species table is implemented, query by actual species ID
            // For now, this returns the total count of all sightings as a placeholder
            // This will need to be updated when the Species table is created
            var allSightings = await _sightingsRepo.GetAllAsync();
            int count = allSightings.Count();

            _logger.LogInformation("Global sighting count for species {SpeciesId}: {Count}", speciesId, count);

            return count;
        }

        public async Task<(int basePoints, double multipler, string name)> GetRarityMultiplierAndName(int globalSightingsCount)
        {
            // BasePoints is a const at the top of this file, check the const field

            if (globalSightingsCount < 0)
            {
                throw new ArgumentException("Global sighting count cannot be negative", nameof(globalSightingsCount));
            }

            // Return value initialization
            double multiplier = 0.0;
            string rarityName = "hi";

            if (globalSightingsCount <= MythicThreshold)
            {
                // Mythic scoring
                multiplier = MythicMultiplier;
                rarityName = "Mythic";

            } else if (globalSightingsCount <= RareThreshold) {

                // Rare scoring
                multiplier = RareMultiplier;
                rarityName = "Rare";

            } else {
                // Common scoring
                multiplier = CommonMultiplier;
                rarityName = "Common";
            }

            return (BasePoints, multiplier, rarityName);
        }
    }
}