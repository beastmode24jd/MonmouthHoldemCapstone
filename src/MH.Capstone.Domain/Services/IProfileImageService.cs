public interface IProfileImageService
{
    Task<byte[]?> ConvertToBytesAsync(IFormFile file);
}