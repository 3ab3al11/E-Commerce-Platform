using System.Diagnostics;
using System.Threading.Tasks;
using ECommerce522.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce522.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
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

        public async Task<IActionResult> Index(ProductFilterVM productFilterVM)
        {
            var products = await _productRepository.GetAsync(includes: [e => e.Category, e => e.Brand], tracked: false);

            // Add Filter
            if (productFilterVM.productName is not null)
            {
                var productNameTrimmed = productFilterVM.productName.Trim();

                products = products.Where(e => e.Name.Contains(productNameTrimmed));
                ViewBag.ProductName = productFilterVM.productName;
            }

            if (productFilterVM.minPrice is not null)
            {
                products = products.Where(e => e.Price > productFilterVM.minPrice);
                ViewBag.MinPrice = productFilterVM.minPrice;
            }

            if (productFilterVM.maxPrice is not null)
            {
                products = products.Where(e => e.Price < productFilterVM.maxPrice);
                ViewBag.MaxPrice = productFilterVM.maxPrice;
            }

            if (productFilterVM.lessQuantity)
            {
                products = products.OrderBy(e => e.Quantity);
                ViewBag.LessQuantity = productFilterVM.lessQuantity;
            }

            if (productFilterVM.status)
            {
                products = products.Where(e => e.Status);
                ViewBag.Status = productFilterVM.status;
            }

            if (productFilterVM.categoryId is not null)
            {
                products = products.Where(e => e.CategoryId == productFilterVM.categoryId);
                ViewBag.CategoryId = productFilterVM.categoryId;
            }

            if (productFilterVM.brandId is not null)
            {
                products = products.Where(e => e.BrandId == productFilterVM.brandId);
                ViewBag.BrandId = productFilterVM.brandId;
            }

            ViewBag.Categories = await _categoryRepository.GetAsync(tracked: false);
            ViewBag.Brands = await _brandRepository.GetAsync(tracked: false);

            // Add Pagination
            var totalNumberOfPages = Math.Ceiling(products.Count() / 8.0);
            ViewBag.totalNumberOfPages = totalNumberOfPages;
            ViewBag.currentPage = productFilterVM.page;

            products = products.Skip((productFilterVM.page - 1) * 8).Take(8);


            return View(products);
        }

        public IActionResult Details(int id)
        {
            var product = _context.Products.Find(id);

            if (product is null)
                return NotFound();

            var relatedProducts = _context.Products.Include(e=>e.Category).Where(e => e.CategoryId == product.CategoryId && e.Id != product.Id).Skip(0).Take(4);

            //var relatedProducts = _context.Products.FromSqlRaw("EXEC sp_find_related_products").ToList();

            return View(new
            {
                Product = product,
                RelatedProducts = relatedProducts
            });
        }
         
        public IActionResult Privacy()
        {
            return View();
        }

        public ViewResult Welcome()
        {
            return View(viewName: "Hello");
        }

        public ViewResult PersonalInfo()
        {
            List<Person> persons = [
                new Person() {
                    Id = 1,
                    Age = 25,
                    Name = "Ali",
                    Skills = ["C#", "C++", "SQL SERVER"]
                },
                new Person() {
                    Id = 2,
                    Age = 26,
                    Name = "Mahmoud",
                    Skills = ["JS", "TypeScript", "Angular"]
                },
                new Person() {
                    Id = 3,
                    Age = 25,
                    Name = "Mona",
                    Skills = ["Python", "SQL Server", "MS Office"]
                },
            ];

            return View(persons);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
