using ECommerce522.APIV9.DTOs.Requests;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce522.APIV9.Areas.Admin
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class BrandsController : ControllerBase
    {
        private readonly IRepository<Brand> _brandRepository;

        public BrandsController(IRepository<Brand> brandRepository)
        {
            _brandRepository = brandRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            var brands = await _brandRepository.GetAsync(tracked: false);

            // Add Filter

            return Ok(brands.AsEnumerable());
        }

        [HttpPost("")]
        public async Task<IActionResult> Create([FromForm] BrandCreateRequest brandCreateRequest)
        {
            var brand = brandCreateRequest.Adapt<Brand>();

            if (brandCreateRequest.Img is not null && brandCreateRequest.Img.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(brandCreateRequest.Img.FileName);

                // Save Img in wwwroot
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\brand_images", fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    brandCreateRequest.Img.CopyTo(stream);
                }

                // Save Img in Db
                brand.Img = fileName;
            }

            await _brandRepository.CreateAsync(brand);
            await _brandRepository.CommitAsync();

            return CreatedAtAction(nameof(GetOne), new { id = brand.Id }, new SuccessModel
            {
                Message = "Add Brand Successfully"
            });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> GetOne([FromRoute] int id)
        {
            var brand = await _brandRepository.GetOneAsync(e => e.Id == id);

            if (brand is null) return NotFound();

            return Ok(brand);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Edit(int id, [FromForm] BrandUpdateRequest brandUpdateRequest)
        {
            var brandInDb = await _brandRepository.GetOneAsync(e => e.Id == id);

            if (brandInDb is null) NotFound();

            if (brandUpdateRequest.Img is not null && brandUpdateRequest.Img.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(brandUpdateRequest.Img.FileName);

                // Save Img in wwwroot
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\brand_images", fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    brandUpdateRequest.Img.CopyTo(stream);
                }

                // Delete Old Img from wwwroot
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\brand_images", brandInDb.Img);

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }

                // Save Img in Db
                brandInDb.Img = fileName;
            }

            //_brandRepository.Update(brand);

            brandInDb.Name = brandUpdateRequest.Name;
            brandInDb.Description = brandUpdateRequest.Description;
            brandInDb.Status = brandUpdateRequest.Status;

            await _brandRepository.CommitAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _brandRepository.GetOneAsync(e => e.Id == id);

            if (brand is null) return NotFound();

            // Delete Old Img from wwwroot
            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\brand_images", brand.Img);

            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }

            _brandRepository.Delete(brand);
            await _brandRepository.CommitAsync();

            return NoContent();
        }
    }
}
