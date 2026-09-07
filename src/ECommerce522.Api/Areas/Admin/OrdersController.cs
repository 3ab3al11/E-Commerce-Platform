using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce522.APIV9.Areas.Admin
{
    [Route("api/[Area]/[controller]")]
    [ApiController]
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class OrdersController : ControllerBase
    {
        private readonly IRepository<Order> _orderRepository;

        public OrdersController(IRepository<Order> orderRepository)
        {
            _orderRepository = orderRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Get(string userName, OrderStatus orderStatus, int page = 1)
        {
            var orders = await _orderRepository.GetAsync(includes: [e => e.ApplicationUser], tracked: false);

            UserFilterResponse userFilterResponse = new();

            // Add Filter
            if (userName is not null)
            {
                orders = orders.Where(e => e.ApplicationUser.Name.Contains(userName));
                userFilterResponse.UserName = userName;
            }

            orders = orders.Where(e => e.OrderStatus == orderStatus);

            // Add Pagination
            var totalNumberOfPages = Math.Ceiling(orders.Count() / 8.0);
            userFilterResponse.TotalNumberOfPages = totalNumberOfPages;
            userFilterResponse.CurrentPage = page;

            orders = orders.Skip((page - 1) * 8).Take(8);

            return Ok(new
            {
                Orders = orders,
                UserFilterResponse = userFilterResponse
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var order = await _orderRepository.GetOneAsync(e => e.Id == id, includes: [e => e.ApplicationUser], tracked: false);

            if (order is null) return NotFound();

            return Ok(order);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Shipped(int id, Order order)
        {
            var orderInDb = await _orderRepository.GetOneAsync(e => e.Id == id, includes: [e => e.ApplicationUser]);

            if (orderInDb is null) return NotFound();

            orderInDb.CarrierId = order.CarrierId;
            orderInDb.CarrierName = order.CarrierName;
            orderInDb.ShippedAt = DateTime.Now;

            await _orderRepository.CommitAsync();

            return NoContent();
        }
    }
}
