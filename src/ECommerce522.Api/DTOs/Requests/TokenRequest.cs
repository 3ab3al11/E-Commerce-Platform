namespace ECommerce522.APIV9.DTOs.Requests
{
    public class TokenRequest
    {
        public string ExpiredAccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
