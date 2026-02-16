using Microsoft.AspNetCore.Http;

namespace MH.Capstone.Domain.Services;

public interface IProfileImageService
{
    Task<byte[]?> ConvertToBytesAsync(IFormFile? file);
}