using AutoMapper;
using Ecom.API.Helper;
using Ecom.Core.DTO;
using Ecom.Core.Entities.Product;
using Ecom.Core.interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

//we should Return CategoryDTO in GetAll & GetById 
// Never expose your Domain Entity to the outside world 


namespace Ecom.API.Controllers
{
  
    public class CategoryController : BaseController
    {
        public CategoryController(IUnitOfWork work , IMapper mapper ) : base (work , mapper)
        {
           
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var categories = await work.CategoryRepository.GetAllAsync();

                return Ok(categories);


            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Get-By-Id/{Id}")]
        public async Task<IActionResult> GetById(int Id)
        {
            try
            {
                var Category = await work.CategoryRepository.GetById(Id);

                if (Category == null) return NotFound(new ResponseAPI(404, $"Not Found Category Id = {Id}"));
                      
                return Ok(Category);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
             
            }

        }

        [HttpPost("Add-Category")]
        public async Task<IActionResult> Add(AddCategoryDto AddCategoryDto)
        {
            try
            {
                var category = mapper.Map<Category>(AddCategoryDto);

                await work.CategoryRepository.AddAsync(category);

                return StatusCode(201,new ResponseAPI(201, "Category created successfully."));


            }
            catch (Exception ex )
            {

                return StatusCode(500, ex.Message);
            }

        }

        [HttpPut("Update-Category")]
        public async Task<IActionResult> Update(UpdateCategoryDto updateCategoryDto)
        {
            try
            {
                var category = mapper.Map<Category>(updateCategoryDto);

                await work.CategoryRepository.UpdateAsync(category);

                return Ok(new ResponseAPI(200, "Category updated successfully"));

            }
            catch (Exception ex)
            {

                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("Delete-Category/{Id}")]

        public async Task<IActionResult> Delete(int Id)
        {
            try
            {
                await work.CategoryRepository.Delete(Id);

                return Ok(new ResponseAPI(200, "Category deleted successfully"));

            }
            catch (Exception ex)
            {

                return StatusCode(500, ex.Message);
            }
        }

    }
}
