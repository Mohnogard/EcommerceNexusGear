using NexusGear.Models;

namespace NexusGear.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Product> DiscountedProducts { get; set; } = new();
        public List<Product> NewArrivals { get; set; } = new();
        public List<Testimonial> Testimonials { get; set; } = new();
    }
}
