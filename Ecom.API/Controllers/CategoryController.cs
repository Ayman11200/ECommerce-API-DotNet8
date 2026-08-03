using AutoMapper;
using Ecom.API.Helper;
using Ecom.Core.Dto;
using Ecom.Core.Entities.Product;
using Ecom.Core.interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

//we should Return CategoryDTO in GetAll & GetByIdAsync 
// Never expose your Domain Entity to the outside world 


namespace Ecom.API.Controllers
{
  
    public class CategoryController : BaseController
    {
        public CategoryController(IUnitOfWork work , IMapper mapper ) : base (work , mapper)
        {
           
        }

        [HttpGet("get-all")]
        [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {

            var categories = await work.CategoryRepository.GetAllAsync();

            var categoriesDto = mapper.Map<IReadOnlyList<CategoryDto>>(categories);

            return Ok(categoriesDto);

        }

        [HttpGet("Get-By-Id/{Id}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(int Id)
        {

            var Category = await work.CategoryRepository.GetByIdAsync(Id);

            if (Category is null) return NotFound(new ResponseAPI(404, $"Not Found Category Id = {Id}"));

            var CategoryDto = mapper.Map<CategoryDto>(Category);

            return Ok(CategoryDto);
     
        }

        [HttpPost("Add-Category")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add(AddCategoryDto AddCategoryDto)
        {

            var category = mapper.Map<Category>(AddCategoryDto);

            await work.CategoryRepository.AddAsync(category);
            await work.SaveChangesAsync();

            var createdDto = mapper.Map<CategoryDto>(category);

            return CreatedAtAction(
                   nameof(GetByIdAsync),
                   new { id = category.Id },
                     createdDto);
        }

        [HttpPut("Update-Category")]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int Id, CategoryDto CategoryDto)
        {
            if(Id != CategoryDto.Id)
                return BadRequest(new ResponseAPI(400, "Route id does not match body id."));

            var category = await work.CategoryRepository.GetByIdAsync(Id);

            if (category is null)
                return NotFound(new ResponseAPI(404, $"Category with Id = {Id} not found."));

            mapper.Map(CategoryDto , category);

            work.CategoryRepository.Update(category);
            await work.SaveChangesAsync();

            return Ok(new ResponseAPI(200, "Category updated successfully"));

        }

        [HttpDelete("Delete-Category/{Id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int Id)
        {

             var Deleted = await work.CategoryRepository.DeleteAsync(Id); ;

            if (!Deleted) return NotFound(new ResponseAPI(404, $"Category with Id = {Id} not found."));

            await work.SaveChangesAsync();

            return NoContent();

        }

    }
}
