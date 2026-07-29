using Ecom.Core.Entities.Product;
using AutoMapper;
using Ecom.Core.interfaces;
using Ecom.infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecom.Core.Services;

namespace Ecom.infrastructure.Repositires
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly AppDbContext context; 
        private readonly IMapper mapper;
        private readonly IImageManagementService imageManagementService;
        public ProductRepository(AppDbContext context ,IMapper mapper ,IImageManagementService imageManagementService) : base(context)
        {
            this.context = context;
            this.mapper = mapper;
            this.imageManagementService = imageManagementService;
        }


        public async Task<Product> AddAsync(AddProductDto addProductDto)
        {
            var newProduct = mapper.Map<Product>(addProductDto);

            await context.Products.AddAsync(newProduct);

            if (addProductDto.Photos != null && addProductDto.Photos.Any())
            {
                var ImagePaths = await imageManagementService.AddImageAsync(addProductDto.Photos, addProductDto.Name);

                newProduct.Photos = ImagePaths.Select(path => new Photo
                {
                    ImageName = path
                }).ToList();          
            }
           
            return newProduct;
        }



        public async Task<bool> UpdateAsync( UpdateProductDto updateProductDto)
        {
            var product = await context.Products.Include(p => p.Photos).FirstOrDefaultAsync(p => p.Id == updateProductDto.Id);

            if (product == null) return false;

            mapper.Map( updateProductDto , product );

            if (updateProductDto is not null && updateProductDto.Photos.Any()) 
            {

                foreach (var oldPhoto in product.Photos)
                {
                    imageManagementService.DeleteImage(oldPhoto.ImageName);
                }
                context.Photos.RemoveRange(product.Photos);

                var ImagePathes = await imageManagementService.AddImageAsync(updateProductDto.Photos, updateProductDto.Name);
                

                var newPhotos = ImagePathes.Select(path => new Photo
                {
                    ImageName = path,
                    ProductId = product.Id,
                });

                await context.Photos.AddRangeAsync(newPhotos);
            }
            return true;

        }
     

        public async Task DeleteAsync(Product product)
        {
            var images = await context.Photos.Where(x => x.ProductId == product.Id)
                .ToListAsync();

            foreach (var image in images)
            {
                imageManagementService.DeleteImage(image.ImageName);
            }
            context.Photos.RemoveRange(images);
            context.Products.Remove(product);

        }

       
    }
}
