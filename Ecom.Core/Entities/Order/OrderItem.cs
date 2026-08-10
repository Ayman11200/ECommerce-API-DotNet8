using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecom.Core.Entities.Order
{
    public class OrderItem : BaseEntity<int>
    {
        public OrderItem()
        {

        }

        public OrderItem(int productItemId, string productName, string mainImage, int quantity, decimal price)
        {
            ProductItemId = productItemId;
            ProductName = productName;
            MainImage = mainImage;
            Quantity = quantity;
            Price = price;
        }

        [Required]
        public int ProductItemId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; }

        [Required]
        [MaxLength(300)]
        public string MainImage { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int OrderId { get; set; }

    }
}