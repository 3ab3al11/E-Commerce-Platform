using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace ECommerce522.APIV9.Areas.Customer
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Customer")]
    [Authorize]
    public class CartsController : ControllerBase
    {
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Promotion> _promotionRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<Order> _orderRepository;

        public CartsController(IRepository<Cart> cartRepository, IRepository<Product> productRepository, IRepository<Promotion> promotionRepository, UserManager<ApplicationUser> userManager, IRepository<Order> orderRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _promotionRepository = promotionRepository;
            _userManager = userManager;
            _orderRepository = orderRepository;
        }

        [HttpGet("{productId}")]
        public async Task<IActionResult> AddToCart(int productId, int count)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var product = await _productRepository.GetOneAsync(e => e.Id == productId);

            if (product is null)
                return NotFound();

            var cartInDb = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.ProductId == productId);

            if (cartInDb is not null)
            {
                cartInDb.Count += count;
            }
            else
            {
                await _cartRepository.CreateAsync(new()
                {
                    ApplicationUserId = user.Id,
                    ProductId = productId,
                    Count = count,
                    ProductPrice = product.Price - (product.Price * (product.Discount / 100m))
                });
            }

            await _cartRepository.CommitAsync();

            return NoContent();
        }

        [HttpGet("Get")]
        public async Task<IActionResult> Get(string code)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cartInDb = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id, includes: [e => e.Product]);

            if (code is not null)
            {
                var promotion = await _promotionRepository.GetOneAsync(e => e.Code == code && e.IsValid && e.ValidTo > DateTime.UtcNow && e.MaxUsage > 0);

                if (promotion is null)
                    return BadRequest(new ErrorModel()
                    {
                        Code = "Invalid Code",
                        Message = "Invalid Code"
                    });
                else
                {
                    bool founded = false;

                    foreach (var item in cartInDb)
                    {
                        if (item.ProductId == promotion.ProductId)
                        {
                            item.ProductPrice -= (item.ProductPrice * (promotion.Discount / 100));
                            promotion.MaxUsage -= 1;
                            await _cartRepository.CommitAsync();
                            founded = true;
                            return Ok(new SuccessModel()
                            {
                                Message = "Apply Code Successfully"
                            });
                        }
                    }

                    if (!founded)
                        return BadRequest(new ErrorModel()
                        {
                            Code = "Invalid Code",
                            Message = "Invalid Code"
                        });
                }
            }

            return Ok();
        }

        [HttpGet("{productId}/IncrementCount")]
        public async Task<IActionResult> IncrementCount(int productId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cartInDb = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.ProductId == productId);

            if (cartInDb is null)
                return NotFound();

            cartInDb.Count += 1;
            await _cartRepository.CommitAsync();

            return NoContent();
        }

        [HttpGet("{productId}/DecrementCount")]
        public async Task<IActionResult> DecrementCount(int productId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cartInDb = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.ProductId == productId);

            if (cartInDb is null)
                return NotFound();

            if (cartInDb.Count > 1)
            {
                cartInDb.Count -= 1;
                await _cartRepository.CommitAsync();
            }

            return NoContent();
        }

        [HttpGet("{productId}/DeleteItem")]
        public async Task<IActionResult> DeleteItem(int productId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cartInDb = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.ProductId == productId);

            if (cartInDb is null)
                return NotFound();

            _cartRepository.Delete(cartInDb);
            await _cartRepository.CommitAsync();

            return NoContent();
        }

        [HttpGet("Pay")]
        public async Task<IActionResult> Pay()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cartInDb = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id, includes: [e => e.Product]);

            // Create Order
            var order = new Order()
            {
                ApplicationUserId = user.Id,
                TotalPrice = cartInDb.Sum(e => e.ProductPrice * e.Count),
            };
            await _orderRepository.CreateAsync(order);
            await _orderRepository.CommitAsync();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/success/{order.Id}",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/cancel/{order.Id}",
            };

            foreach (var item in cartInDb)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "egp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name,
                            Description = item.Product.Description,
                        },
                        UnitAmount = (long)item.ProductPrice * 100,
                    },
                    Quantity = item.Count,
                });
            }

            var service = new SessionService();
            var session = service.Create(options);

            order.SessionId = session.Id;
            await _orderRepository.CommitAsync();

            return Ok(new
            {
                url = session.Url
            });
        }
    }
}
