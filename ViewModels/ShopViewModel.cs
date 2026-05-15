using NexusGear.Models;

namespace NexusGear.ViewModels
{
    public class ShopViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int? SelectedCategoryId { get; set; }
        public string? SearchQuery { get; set; }
        public string? SortBy { get; set; }
        public bool OnlyDiscounted { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
