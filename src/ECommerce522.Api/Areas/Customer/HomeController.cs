using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce522.APIV9.Areas.Customer
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Customer")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;// = new();

        private readonly IRepository<Product> _productRepository;// = new Repository<Product>();
        private readonly IRepository<Category> _categoryRepository;// = new Repository<Category>();
        private readonly IRepository<Brand> _brandRepository;// = new Repository<Brand>();

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IRepository<Product> productRepository, IRepository<Category> categoryRepository, IRepository<Brand> brandRepository)
        {
            _logger = logger;
            _context = context;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
        }

        [HttpPost("Get")]
        public async Task<IActionResult> Get(ProductFilterRequest productFilterRequest)
        {
            var products = await _productRepository.GetAsync(includes: [e => e.Category, e => e.Brand], tracked: false);

            ProductFilterResponse productFilterResponse = new();

            // Add Filter
            if (productFilterRequest.productName is not null)
            {
                var productNameTrimmed = productFilterRequest.productName.Trim();

                products = products.Where(e => e.Name.Contains(productNameTrimmed));
                productFilterResponse.ProductName = productFilterRequest.productName;
            }

            if (productFilterRequest.minPrice is not null)
            {
                products = products.Where(e => e.Price > productFilterRequest.minPrice);
                productFilterResponse.MinPrice = productFilterRequest.minPrice;
            }

            if (productFilterRequest.maxPrice is not null)
            {
                products = products.Where(e => e.Price < productFilterRequest.maxPrice);
                productFilterResponse.MaxPrice = productFilterRequest.maxPrice;
            }

            if (productFilterRequest.lessQuantity)
            {
                products = products.OrderBy(e => e.Quantity);
                productFilterResponse.LessQuantity = productFilterRequest.lessQuantity;
            }

            if (productFilterRequest.status)
            {
                products = products.Where(e => e.Status);
                productFilterResponse.Status = productFilterRequest.status;
            }

            if (productFilterRequest.categoryId is not null)
            {
                products = products.Where(e => e.CategoryId == productFilterRequest.categoryId);
                productFilterResponse.CategoryId = productFilterRequest.categoryId;
            }

            if (productFilterRequest.brandId is not null)
            {
                products = products.Where(e => e.BrandId == productFilterRequest.brandId);
                productFilterResponse.BrandId = productFilterRequest.brandId;
            }

            // Add Pagination
            var totalNumberOfPages = Math.Ceiling(products.Count() / 8.0);
            productFilterResponse.TotalNumberOfPages = totalNumberOfPages;
            productFilterResponse.CurrentPage = productFilterRequest.page;

            products = products.Skip((productFilterRequest.page - 1) * 8).Take(8);

            return Ok(new
            {
                Products = products,
                ProductFilter = productFilterResponse
            });
        }

        [HttpGet("{id}")]
        public IActionResult Details(int id)
        {
            var product = _context.Products.Find(id);

            if (product is null)
                return NotFound();

            var relatedProducts = _context.Products.Include(e => e.Category).Where(e => e.CategoryId == product.CategoryId && e.Id != product.Id).Skip(0).Take(4);

            //var relatedProducts = _context.Products.FromSqlRaw("EXEC sp_find_related_products").ToList();

            return Ok(new
            {
                Product = product,
                RelatedProducts = relatedProducts
            });
        }

    }
}
