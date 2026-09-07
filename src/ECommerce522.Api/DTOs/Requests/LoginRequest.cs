using System.ComponentModel.DataAnnotations;

namespace ECommerce522.APIV9.DTOs.Requests
{
    public class LoginRequest
    {
        public string EmailOrUserName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
