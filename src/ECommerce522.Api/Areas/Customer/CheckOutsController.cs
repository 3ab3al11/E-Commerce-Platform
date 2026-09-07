using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace ECommerce522.APIV9.Areas.Customer
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Customer")]
    [Authorize]
    public class CheckOutsController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;

        public CheckOutsController(IEmailSender emailSender, IRepository<Order> orderRepository, IRepository<Cart> cartRepository, IRepository<OrderItem> orderItemRepository)
        {
            _emailSender = emailSender;
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _orderItemRepository = orderItemRepository;
        }

        [HttpGet("{id}/Success")]
        public async Task<IActionResult> Success(int id)
        {
            var order = await _orderRepository.GetOneAsync(e => e.Id == id, includes: [e => e.ApplicationUser]);
            if (order is null) return NotFound();

            // Send Email
            await _emailSender.SendEmailAsync(order.ApplicationUser.Email!, "Place Order Successfully", $"<h1>Thanks to Place Your Order - Total Price: {order.TotalPrice}</h1>");

            // Update Order
            var service = new SessionService();
            var sessionInfo = service.Get(order.SessionId);

            order.OrderStatus = OrderStatus.InProcessing;
            order.TransactionId = sessionInfo.PaymentIntentId;

            // Transfer Cart To Order items
            var cartInDb = await _cartRepository.GetAsync(e => e.ApplicationUserId == order.ApplicationUserId, includes: [e => e.Product]);

            List<OrderItem> items = cartInDb.Select(e => new OrderItem
            {
                OrderId = id,
                ProductId = e.ProductId,
                Count = e.Count,
                ProductPrice = e.ProductPrice
            }).ToList();

            foreach (var item in items)
                await _orderItemRepository.CreateAsync(item);

            await _orderItemRepository.CommitAsync();

            // Decrease Product Quantity
            foreach (var item in cartInDb)
                item.Product.Quantity -= item.Count;

            // Remove Cart
            foreach (var item in cartInDb)
                _cartRepository.Delete(item);

            await _cartRepository.CommitAsync();

            // Return
            return Created();
        }

        [HttpGet("{id}/Cancel")]
        public IActionResult Cancel(int id)
        {
            return Ok();
        }
    }
}
