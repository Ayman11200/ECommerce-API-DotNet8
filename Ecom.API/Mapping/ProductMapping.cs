using AutoMapper;
using Ecom.Core.Dto;
using Ecom.Core.Entities.Product;

namespace Ecom.API.Mapping
{
    public class ProductMapping : Profile
    {

        public ProductMapping()
        {
            CreateMap<Product, ProductDto>()
    .ForMember(dest => dest.CategoryName,
        opt => opt.MapFrom(src => src.Category.Name));
            


            CreateMap<Photo, PhotoDto>();


            CreateMap<AddProductDto, Product>()
                .ForMember(dest => dest.Photos,
                    opt => opt.Ignore());


    //        CreateMap<UpdateProductDto, Product>()
    //.ForMember(x => x.Photos, op => op.Ignore())
    //.ForAllMembers(opt =>
    //    opt.Condition((src, dest, srcMember, destMember, context)
    //        => srcMember != null));


            
        }

    }
}
