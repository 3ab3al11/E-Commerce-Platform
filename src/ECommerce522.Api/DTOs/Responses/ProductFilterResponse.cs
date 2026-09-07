namespace ECommerce522.APIV9.DTOs.Responses
{
    public class ProductFilterResponse
    {
        public string? ProductName { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool LessQuantity { get; set; }
        public bool Status { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int CurrentPage { get; set; }
        public double TotalNumberOfPages { get; set; }
    }
}
