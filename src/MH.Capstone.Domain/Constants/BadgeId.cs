namespace MH.Capstone.Domain.Constants
{
    public static class BadgeId
    {
            // Badge GUIDs

            // PROFILE CUSTOMIZATION *************
            public static readonly Guid ProfileBadgeGUID = Guid.Parse("A1B2C3D4-E5F6-4789-8A9B-0C1D2E3F4A5B");
            public static readonly Guid CustomBioBadgeGUID = Guid.Parse("91E7773E-F6D7-457E-911E-8246891D65A2");

            // SIGHTINGS *************************
            public static readonly Guid FirstSightingBadgeGUID = Guid.Parse("B2C3D4E5-F6A7-4890-9B0C-1D2E3F4B5A6F");
            
            // 5 Sightings
            public static readonly Guid SightingNoviceBadgeGUID = Guid.Parse("27857EC5-189E-46E8-BE28-871123607F20");

            // 25 Sightings
            public static readonly Guid SightingStudentBadgeGUID = Guid.Parse("8436745D-C25B-44BF-A0E1-0C87E6122724");

            // ******* NEW BADGES TO ADD IN [ NOT ALL AT ONCE ] *******
            
            // SCORING **************

            // First mythic Sighting upload
            // public static readonly Guid MythicSnapshotBadgeGUID = Guid.Parse("");

            // First rare Sighting upload
            // public static readonly Guid RareSnapshotBadgeGUID = Guid.Parse("");

            // First common Sighting upload
            // public static readonly Guid CommonSnapshotBadgeGUID = Guid.Parse("");
            
            // ANIDEX **************

            // 5 different animals in the Anidex
            public static readonly Guid AnidexBeginnerBadgeGUID = Guid.Parse("C3D4E5F6-A7B8-4901-AC1D-2E3F4B5A6F7E");

            // 10 different animals in the Anidex
            // public static readonly Guid AnidexScribeBadgeGUID = Guid.Parse("");
    }
}