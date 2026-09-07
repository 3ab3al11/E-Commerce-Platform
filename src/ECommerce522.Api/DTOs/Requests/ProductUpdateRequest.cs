namespace ECommerce522.APIV9.DTOs.Requests
{
    public class ProductUpdateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public int Quantity { get; set; }
        public bool Status { get; set; } = true;
        public int CategoryId { get; set; }
        public int BrandId { get; set; }
        public IFormFile? Img { get; set; }
        public List<IFormFile>? SubImgs { get; set; }
    }
}
