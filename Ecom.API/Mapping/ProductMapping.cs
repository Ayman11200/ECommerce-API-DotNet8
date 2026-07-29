using AutoMapper;
using Ecom.Core.DTO;
using Ecom.Core.Entities.Product;

namespace Ecom.API.Mapping
{
    public class ProductMapping : Profile
    {

        public ProductMapping()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(x => x.CategoryName,
                op => op.MapFrom(Source => Source.Category.Name))
                .ReverseMap();

            CreateMap<Photo, PhotoDto>().ReverseMap();

            CreateMap<AddProductDto, Product>().ForMember(x => x.Photos, op => op.Ignore()).ReverseMap();

            CreateMap<UpdateProductDto, Product>().ForMember(x => x.Photos, op => op.Ignore()).ReverseMap();
        }

    }
}
