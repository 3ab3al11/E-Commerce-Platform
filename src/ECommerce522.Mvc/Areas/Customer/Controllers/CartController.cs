using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Threading.Tasks;

namespace ECommerce522.Areas.Customer.Controllers
{
    [Authorize]
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Promotion> _promotionRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(IRepository<Cart> cartRepository, IRepository<Product> productRepository, IRepository<Promotion> promotionRepository, UserManager<ApplicationUser> userManager)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _promotionRepository = promotionRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> AddToCart(int productId, int count)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var product = await _productRepository.GetOneAsync(e => e.Id == productId);

            if(product is null)
                return NotFound();

            var cartInDb = await _cartRepository.GetOneAsync(e=>e.ApplicationUserId == user.Id && e.ProductId == productId);

            if(cartInDb is not null)
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

            return RedirectToAction("index");
        }

        public async Task<IActionResult> Index(string code)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cartInDb = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id, includes: [e => e.Product]);

            if(code is not null)
            {
                var promotion = await _promotionRepository.GetOneAsync(e => e.Code == code && e.IsValid && e.ValidTo > DateTime.UtcNow && e.MaxUsage > 0);

                if (promotion is null)
                    TempData["error-notification"] = "Invalid Code";
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
                            TempData["success-notification"] = "Apply Code Successfully";
                            founded = true;
                            break;
                        }
                    }

                    if (!founded)
                        TempData["error-notification"] = "Invalid Code";
                }
            }

            return View(cartInDb);
        }

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

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DecrementCount(int productId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cartInDb = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.ProductId == productId);

            if (cartInDb is null)
                return NotFound();

            if(cartInDb.Count > 1)
            {
                cartInDb.Count -= 1;
                await _cartRepository.CommitAsync();
            }

            return RedirectToAction(nameof(Index));
        }

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

            return RedirectToAction(nameof(Index));

        }

        public async Task<IActionResult> Pay()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cartInDb = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id, includes: [e => e.Product]);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/success",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/cancel",
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
            return Redirect(session.Url);
        }
    }
}
