using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.Entities.Product
{
    public class Product : BaseEntity<int>
    {

        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal NewPrice { get; set; }

        public decimal OldPrice { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; }
        public virtual List<Photo> Photos { get; set; }

        public double Rating { get; set; }

    }
}
