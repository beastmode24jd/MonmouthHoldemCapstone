namespace MH.Capstone.Domain.Constants
{
    public static class BadgeId
    {
            // Initialize consistent GUIDS for the badges
            public static readonly Guid ProfileBadgeGUID = Guid.Parse("A1B2C3D4-E5F6-4789-8A9B-0C1D2E3F4A5B");
            public static readonly Guid CustomBioBadgeGUID = Guid.Parse("91E7773E-F6D7-457E-911E-8246891D65A2");
            public static readonly Guid FirstSightingBadgeGUID = Guid.Parse("B2C3D4E5-F6A7-4890-9B0C-1D2E3F4B5A6F");

            // ******* NEW BADGES TO ADD IN [ NOT ALL AT ONCE ] *******
            public static readonly Guid SightingNoviceBadgeGUID = Guid.Parse(""); // 5 Sightings
            public static readonly Guid SightingExpertBadgeGUID = Guid.Parse(""); // 25 Sightings
            public static readonly Guid MythicSnapshotBadgeGUID = Guid.Parse(""); // First mythic Sighting upload
            public static readonly Guid RareSnapshotBadgeGUID = Guid.Parse(""); // First rare Sighting upload
            public static readonly Guid CommonSnapshotBadgeGUID = Guid.Parse(""); // First common Sighting upload
            public static readonly Guid AnidexScribeBadgeGUID = Guid.Parse(""); // 10 different animals in the Anidex
    }
}