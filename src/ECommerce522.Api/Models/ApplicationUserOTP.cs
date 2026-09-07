namespace ECommerce522.APIV9.Models
{
    public class ApplicationUserOTP
    {
        public int Id { get; set; }
        public string OTP { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddMinutes(30);
        public bool isValid { get; set; } = true;

        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
    }
}
