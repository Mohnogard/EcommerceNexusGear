using NexusGear.Models;

namespace NexusGear.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = new();
        public bool IsInWishlist { get; set; }
        public bool UserHasReviewed { get; set; }
    }
}
