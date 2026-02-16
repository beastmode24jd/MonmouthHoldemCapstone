using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface ISightingsService
    {
        Task CreateSightingAsync(Sighting entity);

        Task<Sighting> GetSightingByIdAsync(Guid id);

        bool ValidateImage(IFormFile? imageBuffer);
    }
}
