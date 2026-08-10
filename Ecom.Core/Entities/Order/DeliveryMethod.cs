using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecom.Core.Entities.Order
{
    public class DeliveryMethod : BaseEntity<int>
    {
        public DeliveryMethod()
        {

        }
        public DeliveryMethod(string name, string description, decimal price, string deliveryTime)
        {
            Name = name;
            Description = description;
            Price = price;
            DeliveryTime = deliveryTime;
        }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(50)]
        public string DeliveryTime { get; set; }


    }
}