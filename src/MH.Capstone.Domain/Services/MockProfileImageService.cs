public class MockProfileImageService : IProfileImageService
{
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        return "Not implemented yet";
    }
}