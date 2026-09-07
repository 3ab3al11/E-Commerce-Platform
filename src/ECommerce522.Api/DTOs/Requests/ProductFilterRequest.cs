namespace ECommerce522.APIV9.DTOs.Requests
{
    public record ProductFilterRequest(string? productName, decimal? minPrice, decimal? maxPrice, bool lessQuantity, bool status, int? categoryId, int? brandId, int page = 1);
}
