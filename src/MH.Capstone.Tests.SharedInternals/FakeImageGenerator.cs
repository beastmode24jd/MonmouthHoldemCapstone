using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Microsoft.AspNetCore.Http.Internal;
using SixLabors.ImageSharp.PixelFormats;
using static MH.Capstone.Tests.SharedInternals.RandomData;

namespace MH.Capstone.Tests.SharedInternals
{
    [ExcludeFromCodeCoverage]
    public class FakeImageGenerator : IDisposable
    {
        private readonly List<Stream> _generatedStreams = new List<Stream>();

        public IEnumerable<IFormFile> GetValidImageFiles(int count = 2)
        {
            for (int i = 0; i < count; i++)
            {
                yield return GetValidImage();
            }
        }

        public IFormFile GetValidImage()
        {
            var height = GetRandomIntInRange(1, 25000);
            var width = GetRandomIntInRange(1, 25000);
            using var img = new Image<Rgba32>(width, height); // Cross-platform image creation using ImageSharp
            //using var bitmap = new Bitmap(width, height); // Windows-specific, not cross-platform
            var fileSteam = new MemoryStream();
            _generatedStreams.Add(fileSteam);
            img.SaveAsPng(fileSteam); // Cross-platform image creation using ImageSharp
            //bitmap.Save(fileSteam, ImageFormat.Png); // Windows-specific, not cross-platform
            fileSteam.Position = 0; // Reset stream position to the beginning after writing the image data

            // Create a fake IFormFile using the stream containing the image data
            var formFile = new FormFile(fileSteam, 0, fileSteam.Length, "file", $"{Guid.NewGuid()}.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            return formFile;
        }

        public void Dispose()
        {
            foreach (var stream in _generatedStreams)
            {
                stream.Dispose();
            }
        }
    }

}
