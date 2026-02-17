using Microsoft.AspNetCore.Http;

namespace MH.Capstone.Domain.Services.Abstraction;

public interface IProfileImageService
{
    Task<byte[]?> ConvertToBytesAsync(IFormFile? file);
}