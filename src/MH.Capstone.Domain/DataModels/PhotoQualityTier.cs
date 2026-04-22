namespace MH.Capstone.Domain.DataModels
{
    public enum PhotoQualityTier
    {
        // No logic yet, enum used just a list of values
        Unknown = 0,

        // Blurry, too dark, too bright, or otherwise low-quality.
        Low = 1,

        // Passes resolution gate; neither Low nor High triggers hit.
        Medium = 2,

        // High sharpness, good exposure, long side greater than or equal to 2048 px.
        High = 3,
    }
}
