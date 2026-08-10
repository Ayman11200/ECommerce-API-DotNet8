using Ecom.Core.Entities.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.infrastructure.Data.Config
{
    public class DeliveryMethodConfiguration : IEntityTypeConfiguration<DeliveryMethod>
    {
        public void Configure(EntityTypeBuilder<DeliveryMethod> builder)
        {
            builder.HasIndex(o => o.Name).IsUnique();

            builder.HasData(
          new DeliveryMethod { Id = 1, DeliveryTime = "5 Days", Description = "The fast Delivery in the world", Name = "DHL", Price = 20 },
        new DeliveryMethod { Id = 2, DeliveryTime = "7 Days", Description = "Make your product save", Name = "XXX", Price = 15 }
                );
        }
    }
}
