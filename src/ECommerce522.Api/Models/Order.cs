namespace ECommerce522.APIV9.Models
{
    public enum OrderStatus
    {
        Pending,
        InProcessing,
        Shipped,
        Completed,
        Returned,
        Canceled
    }

    public enum TransactionType
    {
        Stripe,
        Visa,
        Cash
    }

    public class Order
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser ApplicationUser { get; set; } = default!;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalPrice { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public TransactionType TransactionType { get; set; } = TransactionType.Stripe;
        public string SessionId { get; set; } = string.Empty;
        public string? TransactionId { get; set; }

        public string? CarrierName { get; set; }
        public string? CarrierId { get; set; }
        public DateTime ShippedAt { get; set; } 
    }
}
