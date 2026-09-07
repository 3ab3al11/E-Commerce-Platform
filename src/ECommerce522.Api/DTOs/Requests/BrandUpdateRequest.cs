namespace ECommerce522.APIV9.DTOs.Requests
{
    public class BrandUpdateRequest
    {
        public string Name { get; set; } = string.Empty;
        public IFormFile? Img { get; set; }
        public string? Description { get; set; }
        public bool Status { get; set; } = true;
    }
}
