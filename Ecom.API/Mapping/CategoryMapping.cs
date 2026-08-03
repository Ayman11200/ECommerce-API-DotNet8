using AutoMapper;
using Ecom.Core.Dto;
using Ecom.Core.Entities.Product;

namespace Ecom.API.Mapping
{
    public class CategoryMapping : Profile
    {

       public CategoryMapping()
        {
            CreateMap<AddCategoryDto, Category>().ReverseMap();  
            CreateMap<CategoryDto, Category>().ReverseMap();
        }

    }
}
