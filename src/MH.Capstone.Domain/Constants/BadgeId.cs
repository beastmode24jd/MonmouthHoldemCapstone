namespace MH.Capstone.Domain.Constants
{
    public static class BadgeId
    {
            // Badge GUIDs

            // PROFILE CUSTOMIZATION
            public static readonly Guid ProfileBadgeGUID = Guid.Parse("A1B2C3D4-E5F6-4789-8A9B-0C1D2E3F4A5B");
            public static readonly Guid CustomBioBadgeGUID = Guid.Parse("91E7773E-F6D7-457E-911E-8246891D65A2");

            // SIGHTINGS
            public static readonly Guid FirstSightingBadgeGUID = Guid.Parse("B2C3D4E5-F6A7-4890-9B0C-1D2E3F4B5A6F");

            // ******* NEW BADGES TO ADD IN [ NOT ALL AT ONCE ] *******
            // public static readonly Guid SightingNoviceBadgeGUID = Guid.Parse(""); // 5 Sightings
            // public static readonly Guid SightingStudentBadgeGUID = Guid.Parse(""); // 25 Sightings
            
            // First mythic Sighting upload
            public static readonly Guid MythicSnapshotBadgeGUID = Guid.Parse("edead9d9-cdc2-423e-b404-88cc8d15ab38");

            // First rare Sighting upload
            public static readonly Guid RareSnapshotBadgeGUID = Guid.Parse("d55272de-d852-4fc6-8721-4c557259ec03");

            // First common Sighting upload
            public static readonly Guid CommonSnapshotBadgeGUID = Guid.Parse("7e5c3e3c-358f-40ac-8eaf-fe7e1a33677a");
            
            // ANIDEX
            // public static readonly Guid AnidexBeginnerBadgeGUID = Guid.Parse(""); // 5 different animals in the Anidex
            // public static readonly Guid AnidexScribeBadgeGUID = Guid.Parse(""); // 10 different animals in the Anidex
    }
}