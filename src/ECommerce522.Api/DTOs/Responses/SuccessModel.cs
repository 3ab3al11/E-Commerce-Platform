namespace ECommerce522.APIV9.DTOs.Responses
{
    public class SuccessModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
