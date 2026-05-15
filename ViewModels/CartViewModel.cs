using NexusGear.Models;

namespace NexusGear.ViewModels
{
    public class CartViewModel
    {
        public List<CartItem> Items { get; set; } = new();

        public decimal Subtotal => Items.Sum(i => (i.Product?.DiscountedPrice ?? 0) * i.Quantity);
        public decimal ShippingCost => Subtotal >= 50 ? 0 : 9.99m;
        public decimal Total => Subtotal + ShippingCost;
        public int ItemCount => Items.Sum(i => i.Quantity);
    }
}
