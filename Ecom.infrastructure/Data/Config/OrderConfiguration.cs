using Ecom.Core.Entities.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.infrastructure.Data.Config
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Status)
                .HasConversion(
                s => s.ToString(),
                s => (Status)Enum.Parse(typeof(Status), s))
                .HasMaxLength(20)
                .IsRequired();


            builder.HasOne(o => o.DeliveryMethod)
                .WithMany()
                .HasForeignKey(o => o.DeliveryMethodId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.OrderItems)
                .WithOne()
                .HasForeignKey(oi => oi.OrderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.OwnsOne(o => o.ShippingAddress)
                .WithOwner();
            builder.Navigation(o => o.ShippingAddress).IsRequired();

            builder.HasIndex(o => o.BuyerEmail);
            builder.HasIndex(o => new { o.BuyerEmail, o.OrderDate });

        }
    }
}
