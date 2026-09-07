namespace ECommerce522.Models
{
    public class Promotion
    {
        public int Id { get; set; }
        public string? ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public int? ProductId { get; set; }
        public Product Product { get; set; }

        public string Code { get; set; }
        public decimal Discount { get; set; }
        public bool IsValid { get; set; } = true;
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddDays(30);
        public int MaxUsage { get; set; }
    }
}
