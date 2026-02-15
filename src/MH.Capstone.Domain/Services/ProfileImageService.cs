public class ProfileImageService : IProfileImageService
{
    /* Mocked Service for storing profile image uploads.
        Writes to local files, and can have implementation changed for
            saving to a production DB.
    */
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");

    public async Task<byte[]?> ConvertToBytesAsync(IFormFile file)
    {
        if ( file == null || file.Length == 0 )
        {
            return null;
        }

        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
}