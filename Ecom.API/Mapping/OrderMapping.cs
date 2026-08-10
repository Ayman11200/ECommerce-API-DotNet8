using AutoMapper;
using Ecom.Core.DTO;
using Ecom.Core.Entities.Order;

namespace Ecom.API.Mapping
{
    public class OrderMapping : Profile
    {

        public OrderMapping()
        {

            CreateMap<Order, OrderToReturnDTO>()
                .ForMember(o => o.deliveryMethod,
                o => o.MapFrom(s => s.DeliveryMethod.Name
                )).ReverseMap();

            CreateMap<OrderItem, OrderItemDTO>().ReverseMap();
                

        }


    }
}
