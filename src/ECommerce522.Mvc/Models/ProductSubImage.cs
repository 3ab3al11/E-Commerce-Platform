using Microsoft.EntityFrameworkCore;

namespace ECommerce522.Models
{
    public class ProductSubImage
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;
        public string Img { get; set; } = string.Empty;
    }
}
