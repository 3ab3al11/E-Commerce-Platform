using ECommerce522.APIV9.DTOs.Requests;
using ECommerce522.APIV9.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce522.APIV9.Areas.Admin
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    [Area("Admin")]
    public class ProductsController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IProductSubImageRepository _productSubImageRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Brand> _brandRepository;

        public ProductsController(IRepository<Product> productRepository, IProductSubImageRepository productSubImageRepository, IRepository<Category> categoryRepository, IRepository<Brand> brandRepository)
        {
            _productRepository = productRepository;
            _productSubImageRepository = productSubImageRepository;
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

        [HttpPost("")]
        public async Task<IActionResult> Create([FromForm] ProductCreateRequest productCreateRequest)
        {
            var product = productCreateRequest.Adapt<Product>();

            if (productCreateRequest.Img is not null && productCreateRequest.Img.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(productCreateRequest.Img.FileName);

                // Save Img in wwwroot
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\product_images", fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    productCreateRequest.Img.CopyTo(stream);
                }

                // Save Img in Db
                product.MainImg = fileName;
            }

            await _productRepository.CreateAsync(product);
            await _productRepository.CommitAsync();

            if (productCreateRequest.SubImgs is not null && productCreateRequest.SubImgs.Count > 0)
            {
                foreach (var item in productCreateRequest.SubImgs)
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

            return CreatedAtAction(nameof(GetOne), new { id = product.Id }, new SuccessModel
            {
                Message = "Add Product Successfully"
            });
        }

        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var product = await _productRepository.GetOneAsync(e => e.Id == id, tracked: false);

            var productSubImages = await _productSubImageRepository.GetAsync(e => e.ProductId == id, tracked: false);

            return Ok(new
            {
                Product = product,
                ProductSubImages = productSubImages
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Edit(int id, [FromForm] ProductUpdateRequest productUpdateRequest)
        {
            var productInDb = await _productRepository.GetOneAsync(e => e.Id == id);

            if (productInDb is null) return NotFound();

            if (productUpdateRequest.Img is not null && productUpdateRequest.Img.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(productUpdateRequest.Img.FileName);

                // Save Img in wwwroot
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\product_images", fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    productUpdateRequest.Img.CopyTo(stream);
                }

                // Delete Old Img from wwwroot
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\brand_images", productInDb.MainImg);

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }

                // Save Img in Db
                productInDb.MainImg = fileName;
            }

            productInDb.Name = productUpdateRequest.Name;
            productInDb.Description = productUpdateRequest.Description;
            productInDb.Price = productUpdateRequest.Price;
            productInDb.Quantity = productUpdateRequest.Quantity;
            productInDb.Discount = productUpdateRequest.Discount;
            productInDb.Status = productUpdateRequest.Status;
            productInDb.CategoryId = productUpdateRequest.CategoryId;
            productInDb.BrandId = productUpdateRequest.BrandId;

            await _productRepository.CommitAsync();

            if (productUpdateRequest.SubImgs is not null && productUpdateRequest.SubImgs.Count > 0)
            {
                // Delete Old sub imgs from wwwroot & Db
                var productSubImages = await _productSubImageRepository.GetAsync(e => e.ProductId == id);

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
                foreach (var item in productUpdateRequest.SubImgs)
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
                        ProductId = id
                    });
                }

                await _productSubImageRepository.AddRangeAsync(listOfNewProductSubImages);
                await _productSubImageRepository.CommitAsync();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetOneAsync(e => e.Id == id);

            if (product is null) return NotFound();

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

            return NoContent();
        }
    }
}
