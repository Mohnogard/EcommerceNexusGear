namespace NexusGear.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }
    }
}
