using AutoMapper;
using Ecom.Core.DTO;
using Ecom.Core.Entities;


namespace Ecom.API.Mapping
{
    public class BasketMapping : Profile
    {
        public BasketMapping()
        {
            CreateMap<BasketDto, CustomerBasket>().ReverseMap();
        }

    }
}
