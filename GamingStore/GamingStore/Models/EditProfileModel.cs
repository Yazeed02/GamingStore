namespace GamingStore.Models
{
    public class EditProfileModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string CurrentPassword { get; set; } 
        public string NewPassword { get; set; } 
    }
}