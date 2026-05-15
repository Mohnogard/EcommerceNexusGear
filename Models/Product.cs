using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusGear.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(3000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; } = 0;

        public int Stock { get; set; } = 0;
        public string? ImageUrl { get; set; }
        public string? Slug { get; set; }
        public string? Brand { get; set; }
        public string? Sku { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public bool IsNewArrival { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        [NotMapped]
        public decimal DiscountedPrice => DiscountPercentage > 0
            ? Math.Round(Price * (1 - DiscountPercentage / 100), 2)
            : Price;

        [NotMapped]
        public double AverageRating => Reviews.Any(r => r.IsApproved) ? Reviews.Where(r => r.IsApproved).Average(r => r.Rating) : 0;

        [NotMapped]
        public int ReviewCount => Reviews.Count(r => r.IsApproved);

        [NotMapped]
        public bool IsOutOfStock => Stock <= 0;
    }
}
