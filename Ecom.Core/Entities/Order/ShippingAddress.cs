using System.ComponentModel.DataAnnotations;

namespace Ecom.Core.Entities.Order
{
    public class ShippingAddress 
    {
        public ShippingAddress()
        {

        }
        public ShippingAddress(string firstName, string lastName, string city, string zipCode, string street, string state)
        {
            FirstName = firstName;
            LastName = lastName;
            City = city;
            ZipCode = zipCode;
            Street = street;
            State = state;
        }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; }

        [Required]
        [MaxLength(20)]
        public string ZipCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string Street { get; set; }

        [Required]
        [MaxLength(100)]
        public string State { get; set; }

    }
}