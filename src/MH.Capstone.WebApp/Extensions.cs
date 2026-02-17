using MH.Capstone.Domain.DataModels;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;

namespace MH.Capstone.WebApp
{
    [ExcludeFromCodeCoverage]
    internal static class Extensions
    {
        // TODO - Make this part of the configuration settings to allow for easier updates and
        // potential future changes to the default profile image.
        private const string DefaultProfileImageUrl = "/imgs/profileDefault.jpg";

        public static string GetProfileImageUrl(this ApplicationUser? user)
        {
            // Fetch the user profile image from the Model.
            // Defaults to the placeholder profile image if not found.
            // Convert byte[] to Base64 string for HTML display if custom image exists.
            return (user?.ProfileImage != null && !string.IsNullOrEmpty(user.ProfileImageType)) ?
                $"data:{user.ProfileImageType};base64,{Convert.ToBase64String(user.ProfileImage)}" :
                DefaultProfileImageUrl; // Default profile image URL for users without a custom profile image (or null user obj).
        }
    }
}
