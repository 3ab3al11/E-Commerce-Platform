using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce522.APIV9.Areas.Admin
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class CategoriesController : ControllerBase
    {

        private IRepository<Category> _categoryRepository;

        public CategoriesController(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            var categories = await _categoryRepository.GetAsync(tracked: false);

            // Add Filter

            return Ok(categories.AsEnumerable());
        }

        [HttpPost("")]
        public async Task<IActionResult> Create(Category category)
        {
            //if (!ModelState.IsValid)
            //{
            //    TempData["error-notification"] = "Invalid Data";
            //    return View(category);
            //}

            //CategoryValidator validationRules = new CategoryValidator();
            //var result = validationRules.Validate(category);

            //if (!result.IsValid)
            //    return View(category);

            await _categoryRepository.CreateAsync(category);
            await _categoryRepository.CommitAsync();

            //Response.Cookies.Append("success-notification", "Create Category Successfully", new()
            //{
            //    Expires = DateTime.UtcNow.AddMinutes(30),
            //});
            //HttpContext.Session.SetString("success-notification", "Create Category Successfully");

            return CreatedAtAction(nameof(GetOne), new { id = category.Id } , new SuccessModel
            {
                Message = "Create Category Successfully"
            });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> GetOne([FromRoute] int id)
        {
            var category = await _categoryRepository.GetOneAsync(e => e.Id == id);

            if (category is null) return NotFound();

            return Ok(category);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            var categoryInDB = await _categoryRepository.GetOneAsync(e => e.Id == id);

            //_categoryRepository.Update(category);

            categoryInDB.Name = category.Name;
            categoryInDB.Description = category.Description;
            categoryInDB.Status = category.Status;

            await _categoryRepository.CommitAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetOneAsync(e => e.Id == id);

            if (category is null) return NotFound();

            _categoryRepository.Delete(category);
            await _categoryRepository.CommitAsync();

            return NoContent();
        }
    }
}
