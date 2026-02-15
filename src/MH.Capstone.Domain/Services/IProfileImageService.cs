public interface IProfileImageService
{
    Task<string> UploadImageAsync(IFormFile file);
}