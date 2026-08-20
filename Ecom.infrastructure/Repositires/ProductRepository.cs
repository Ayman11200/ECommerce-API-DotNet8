using AutoMapper;
using AutoMapper.QueryableExtensions;
using Ecom.Core.Entities.Product;
using Ecom.Core.interfaces;
using Ecom.Core.Services;
using Ecom.Core.Sharing;
using Ecom.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<IEnumerable<ProductDto>> GetAllAsync(ProductParams productParams)
        {
            var query = context.Products
                .Include(m => m.Photos)
                .Include(m => m.Category)
                .AsNoTracking();


            if (!string.IsNullOrEmpty(productParams.Search))
            {
                var searchWords = productParams.Search.Split(' ');

                query = query.Where(m => searchWords.All(word =>

                m.Name.ToLower().Contains(word.ToLower()) ||
                m.Description.ToLower().Contains(word.ToLower())

                    )); 
            }



            if (productParams.CategoryId.HasValue)
                query = query.Where(m => m.CategoryId == productParams.CategoryId);


            if (!string.IsNullOrEmpty(productParams.Sort)) 
            {

                query = productParams.Sort switch
                {
                    "PriceAce" => query.OrderBy(m => m.NewPrice),
                    "PriceDce" => query.OrderByDescending(m => m.NewPrice),
                    _ => query.OrderBy(m => m.Name),
                };
            }

            productParams.TotalCount = await query.CountAsync();

            query = query.Skip((productParams.PageSize) * (productParams.PageNumber - 1))
                .Take(productParams.PageSize);

            var result = await query
             .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
             .ToListAsync();

            return result;

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



        public async Task<bool> UpdateAsync(int Id , UpdateProductDto updateProductDto)
        {
            var product = await context.Products.Include(p => p.Photos).FirstOrDefaultAsync(p => p.Id == Id);

            if (product == null) return false;

           
            if (!string.IsNullOrWhiteSpace(updateProductDto.Name))
            {
                product.Name = updateProductDto.Name;
            }

     
            if (!string.IsNullOrWhiteSpace(updateProductDto.Description))
            {
                product.Description = updateProductDto.Description;
            }

            if (updateProductDto.NewPrice.HasValue)
            {
                product.NewPrice = updateProductDto.NewPrice.Value;
            }

            if (updateProductDto.OldPrice.HasValue)
            {
                product.OldPrice = updateProductDto.OldPrice.Value;
            }

            if (updateProductDto.CategoryId.HasValue)
            {
                product.CategoryId = updateProductDto.CategoryId.Value;
            }


            if (updateProductDto.Photos is not null && updateProductDto.Photos.Any()) 
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
