using AutoMapper;
using Ecom.API.Helper;
using Ecom.Core.Dto;
using Ecom.Core.Entities.Product;
using Ecom.Core.interfaces;
using Ecom.Core.Sharing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Ecom.API.Controllers
{

    public class ProductController : BaseController
    {
        public ProductController(IUnitOfWork work, IMapper mapper) : base(work, mapper)
        {

        }



        [HttpGet]
        [ProducesResponseType(typeof(Pagination<ProductDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] ProductParams productParams)
        {
            var productDto = await work.ProductRepository.GetAllAsync(productParams);

            return Ok(new Pagination<ProductDto>(productParams.PageNumber, productParams.PageSize, productParams.TotalCount, productDto));
            
        }


        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(int Id)
        {
            var product = await work.ProductRepository.GetByIdAsync(Id, p => p.Photos, p => p.Category);

            if (product == null) return NotFound(new ResponseAPI(404, $"Not Found Product Id = {Id}"));

            ProductDto productDto = mapper.Map<ProductDto>(product);

            return Ok(productDto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add(AddProductDto addProductDto)
        {
           if (addProductDto is null)
                return BadRequest();

            var product = await work.ProductRepository.AddAsync(addProductDto);

            await work.SaveChangesAsync();

            var createdDto = mapper.Map<ProductDto>(product);

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, createdDto);
        }



        [HttpPut("{Id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int Id, UpdateProductDto updateProductDto)
        {
           
     
            var updated = await work.ProductRepository.UpdateAsync(Id,updateProductDto);

            if(! updated)
                return NotFound(new ResponseAPI(404, $"Product with Id = {Id} not found."));

            await work.SaveChangesAsync();

            return Ok(new ResponseAPI(200, "Product updated successfully"));
        }

        [HttpDelete("{Id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int Id)
        {

            var product = await work.ProductRepository.GetByIdAsync(Id, x => x.Photos);

            if (product is null) return NotFound((new ResponseAPI(404, $"Product with Id = {Id} Not found!")));

            await work.ProductRepository.DeleteAsync(product);

            await work.SaveChangesAsync();

            return NoContent();

        }


    }
}
