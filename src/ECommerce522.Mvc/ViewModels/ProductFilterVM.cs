namespace ECommerce522.ViewModels
{
    public record ProductFilterVM(string? productName, decimal? minPrice, decimal? maxPrice, bool lessQuantity, bool status, int? categoryId, int? brandId, int page = 1);
}
