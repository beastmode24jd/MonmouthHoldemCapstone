namespace MH.Capstone.WebApp.Models
{
    public class ApplicationUser
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        
        // Sets profile icon to default in wwwroot folder if not custom
        public string ProfileImageUrl { get; set; } = "/imgs/profileDefault.jpeg";
    }
}