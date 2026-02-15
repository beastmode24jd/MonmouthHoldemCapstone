public class MockProfileImageService : IProfileImageService
{
    /* Mocked Service for storing profile image uploads.
        Writes to local files, and can have implementation changed for
            saving to a production DB.
    */
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if ( file == null || file.Length == 0 )
        {
            return "/imgs/profileDefault.jpg";
        }

        // Ensure that the path to the image file exists, if image file is present
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }

        // Use GUID (random character string generator?) to create unique file name
        var filename = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filepath = Path.Combine(_storagePath, filename);

        using (var stream = new FileStream(filepath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/profiles/{filename}";
    }
}