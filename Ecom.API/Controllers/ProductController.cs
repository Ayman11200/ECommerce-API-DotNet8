using AutoMapper;
using Ecom.API.Helper;
using Ecom.Core.DTO;
using Ecom.Core.Entities.Product;
using Ecom.Core.interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.API.Controllers
{

    public class ProductController : BaseController
    {
        public ProductController(IUnitOfWork work, IMapper mapper) : base(work, mapper)
        {

        }



        [HttpGet("{Id}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await work.ProductRepository.GetByIdAsync(id, x => x.Photos, x => x.Category);

            if (product == null) return NotFound(new ResponseAPI(404, $"Not Found Product Id = {id}"));

            ProductDto productDto = mapper.Map<ProductDto>(product);

            return Ok(productDto);
        }

        [HttpPost]
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



        [HttpPut("{Id},{entity}")]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int Id, UpdateProductDto updateProductDto)
        {
           
            if (Id != updateProductDto.Id)
                return BadRequest(new ResponseAPI(400, "Route id does not match body id."));

            var updated = await work.ProductRepository.DeleteAsync(Id);

            if(! updated)
                return NotFound(new ResponseAPI(404, $"Product with Id = {Id} not found."));

            await work.SaveChangesAsync();

            return Ok(new ResponseAPI(200, "Product updated successfully"));
        }

        [HttpDelete("{Id}")]
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
