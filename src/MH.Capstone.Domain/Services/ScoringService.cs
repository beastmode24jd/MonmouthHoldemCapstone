using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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

        private (double Multiplier, string Name) GetRarityMetadata(int globalSightingsCount)
        {
            return globalSightingsCount switch
            {
                <= MythicThreshold => (MythicMultiplier, "Mythic"),
                <= RareThreshold => (RareMultiplier, "Rare"),
                _ => (CommonMultiplier, "Common")
            };
        }

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
            // Uses private helper method, since metadata call uses this same code block
            var (multiplier, _) = GetRarityMetadata(globalSightingsCount);

            // Calculate final points
            int points = (int)(BasePoints * multiplier);

            _logger.LogInformation(
                "Calculated {Points} points for sighting (Global Count: {Count}, Multiplier: {Multiplier}×)",
                points, globalSightingsCount, multiplier);

            return await Task.FromResult(points);
        }

        /// <summary>
        /// CSP-108 / CSP-142: Get global sighting count for a species, by name.
        /// Case-insensitive match on Sighting.SpeciesName.
        /// </summary>
        public async Task<int> GetGlobalSightingsCountAsync(string speciesName)
        {
            if (string.IsNullOrWhiteSpace(speciesName))
            {
                throw new ArgumentException("Species name must be non-empty", nameof(speciesName));
            }

            var matches = await _sightingsRepo.GetAllAsync(
                s => s.SpeciesName.ToLower() == speciesName.ToLower());
            int count = matches.Count();

            _logger.LogInformation("Global sighting count for species '{SpeciesName}': {Count}", speciesName, count);

            return count;
        }

        public async Task<(double multipler, string name)> GetRarityMultiplierAndName(int globalSightingsCount)
        {
            if (globalSightingsCount < 0)
            {
                throw new ArgumentException("Global sighting count cannot be negative", nameof(globalSightingsCount));
            }

            // Use the private helper method to get the return values
            var metadata = GetRarityMetadata(globalSightingsCount);

            // Return the full tuple from this
            return await Task.FromResult((metadata.Multiplier, metadata.Name));
        }
    }
}