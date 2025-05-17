using System.ComponentModel.DataAnnotations;

namespace GamingStore.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product Name is required")]
        [StringLength(150, ErrorMessage = "Product name must be 150 characters or less.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 10000, ErrorMessage = "Price must be between $0.01 and $10,000.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public ProductCategory Category { get; set; }

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, 1000, ErrorMessage = "Stock must be between 0 and 1000.")]
        public int Stock { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsOnSale { get; set; }

        [Range(1, 100, ErrorMessage = "Discount must be between 1% and 100%.")]
        public int? DiscountPercentage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;


        [Required(ErrorMessage = "Images is required")]
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductComment> Comments { get; set; } = new List<ProductComment>();
        public ICollection<ProductRating> Ratings { get; set; } = new List<ProductRating>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    public enum ProductCategory
    {
        [Display(Name = "Gaming PCs")]
        GamingPCs,
        [Display(Name = "Gaming Laptops")]
        GamingLaptops,
        [Display(Name = "PlayStation")]
        PlayStation,
        [Display(Name = "Xbox")]
        Xbox,
        [Display(Name = "Nintendo")]
        Nintendo,
        [Display(Name = "Gaming Chairs")]
        GamingChairs,
        [Display(Name = "Gaming Desks")]
        GamingDesks,
        [Display(Name = "Monitors")]
        Monitors,
        [Display(Name = "Keyboards")]
        Keyboards,
        [Display(Name = "Microphone")]
        Mice,
        [Display(Name = "Headsets")]
        Headsets,
        [Display(Name = "Controllers")]
        Controllers,
        [Display(Name = "VR Headsets")]
        VRHeadsets,
        [Display(Name = "Gaming Accessories")]
        GamingAccessories,
        [Display(Name = "PC Components")]
        PCComponents,
        [Display(Name = "Gaming Merchandise")]
        GamingMerchandise
    }
}
