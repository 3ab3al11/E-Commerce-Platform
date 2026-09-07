using ECommerce522.Models;
using ECommerce522.Repositories;
using ECommerce522.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ECommerce522.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class ProductController : Controller
    {
        //ApplicationDbContext _context = new();
        private readonly IRepository<Product> _productRepository;// = new Repository<Product>();
        private readonly IProductSubImageRepository _productSubImageRepository;// = new ProductSubImageRepository();
        private readonly IRepository<Category> _categoryRepository;// = new Repository<Category>();
        private readonly IRepository<Brand> _brandRepository;// = new Repository<Brand>();

        public ProductController(IRepository<Product> productRepository, IProductSubImageRepository productSubImageRepository, IRepository<Category> categoryRepository, IRepository<Brand> brandRepository)
        {
            _productRepository = productRepository;
            _productSubImageRepository = productSubImageRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
        }

        public async Task<IActionResult> Index(ProductFilterVM productFilterVM)
        {
            var products = await _productRepository.GetAsync(includes: [e => e.Category, e => e.Brand], tracked: false);

            // Add Filter
            if(productFilterVM.productName is not null)
            {
                var productNameTrimmed = productFilterVM.productName.Trim();

                products = products.Where(e=>e.Name.Contains(productNameTrimmed));
                ViewBag.ProductName = productFilterVM.productName;
            }

            if(productFilterVM.minPrice is not null)
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
                products = products.Where(e=>e.Status);
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

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _categoryRepository.GetAsync(tracked: false);
            ViewBag.Brands = await _brandRepository.GetAsync(tracked: false);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile Img, List<IFormFile> SubImgs)
        {
            if (Img is not null && Img.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Img.FileName);

                // Save Img in wwwroot
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\product_images", fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    Img.CopyTo(stream);
                }

                // Save Img in Db
                product.MainImg = fileName;
            }

            await _productRepository.CreateAsync(product);
            await _productRepository.CommitAsync();

            if (SubImgs is not null && SubImgs.Count > 0)
            {
                foreach (var item in SubImgs)
                {
                    // Save Img in wwwroot
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(item.FileName);

                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\product_images\\product_sub_images", fileName);

                    using (var stream = System.IO.File.Create(filePath))
                    {
                        item.CopyTo(stream);
                    }

                    // Save Img in Db
                    await _productSubImageRepository.CreateAsync(new()
                    {
                        Img = fileName,
                        ProductId = product.Id
                    });
                }

                await _productSubImageRepository.CommitAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetOneAsync(e => e.Id == id, tracked: false);

            ViewBag.Categories = await _categoryRepository.GetAsync(tracked: false);
            ViewBag.Brands = await _brandRepository.GetAsync(tracked: false);
            ViewBag.ProductSubImages = await _productSubImageRepository.GetAsync(e => e.ProductId == id, tracked: false);

            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Edit(Product product, IFormFile? Img, List<IFormFile>? SubImgs)
        {
            var productInDb = await _productRepository.GetOneAsync(e => e.Id == product.Id, tracked: false);

            if (productInDb is null)
                return RedirectToAction(nameof(HomeController.NotFoundPage), "Home");

            if (Img is not null && Img.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Img.FileName);

                // Save Img in wwwroot
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\product_images", fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    Img.CopyTo(stream);
                }

                // Delete Old Img from wwwroot
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\brand_images", productInDb.MainImg);

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }

                // Save Img in Db
                product.MainImg = fileName;
            }
            else
            {
                product.MainImg = productInDb.MainImg;
            }

            _productRepository.Update(product);
            await _productRepository.CommitAsync();

            if (SubImgs is not null && SubImgs.Count > 0)
            {
                // Delete Old sub imgs from wwwroot & Db
                var productSubImages = await _productSubImageRepository.GetAsync(e => e.ProductId == product.Id);

                List<ProductSubImage> listOfProductSubImages = [];
                foreach (var item in productSubImages)
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\brand_images", item.Img);

                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                    listOfProductSubImages.Add(item);
                }

                _productSubImageRepository.RemoveRange(listOfProductSubImages);
                await _productSubImageRepository.CommitAsync();

                // Create & Save New sub imgs
                List<ProductSubImage> listOfNewProductSubImages = [];
                foreach (var item in SubImgs)
                {
                    // Save Img in wwwroot
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(item.FileName);

                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\product_images\\product_sub_images", fileName);

                    using (var stream = System.IO.File.Create(filePath))
                    {
                        item.CopyTo(stream);
                    }

                    // Save Img in Db
                    listOfNewProductSubImages.Add(new()
                    {
                        Img = fileName,
                        ProductId = product.Id
                    });
                }

                await _productSubImageRepository.AddRangeAsync(listOfNewProductSubImages);
                await _productSubImageRepository.CommitAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetOneAsync(e => e.Id == id);

            if (product is null)
                return RedirectToAction(nameof(HomeController.NotFoundPage), "Home");

            // Delete Old Img from wwwroot
            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\product_images", product.MainImg);

            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }

            // Delete Old sub imgs from wwwroot & Db
            var productSubImages = await _productSubImageRepository.GetAsync(e => e.ProductId == product.Id);

            List<ProductSubImage> listOfProductSubImages = [];
            foreach (var item in productSubImages)
            {
                var oldSubImgFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\product_images\\product_sub_images", item.Img);

                if (System.IO.File.Exists(oldSubImgFilePath))
                {
                    System.IO.File.Delete(oldSubImgFilePath);
                }

                listOfProductSubImages.Add(item);
            }

            _productSubImageRepository.RemoveRange(listOfProductSubImages);
            await _productSubImageRepository.CommitAsync();

            _productRepository.Delete(product);
            await _productRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
