using System.ComponentModel.DataAnnotations;

namespace ECommerce522.APIV9.DTOs.Requests
{
    public class ResetPasswordRequest
    {
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
