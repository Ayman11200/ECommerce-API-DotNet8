using Ecom.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.DTO
{
    public class BasketDto
    {
        public string Id { get; init; }
        public List<BasketItem> BasketItems { get; init; }

        //public class BasketItemDTO
        //{
        //    public int Id { get; set; }
        //    public int ProductId { get; set; }
        //    public string Name { get; set; }
        //    public string Description { get; set; }
        //    public int Quantity { get; set; }
        //    public string Image { get; set; }
        //    public decimal Price { get; set; }
        //    public string CategoryName { get; set; }
        //}
    }


}

