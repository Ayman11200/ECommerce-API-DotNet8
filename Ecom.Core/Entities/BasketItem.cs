namespace Ecom.Core.Entities
{
    public class BasketItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public int Quantity { get; set; }

        public string Image { get; set; } = "";

        public decimal Price { get; set; }

        public string CategoryName { get; set; } = "";

    }
}