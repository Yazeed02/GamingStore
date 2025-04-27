using System.ComponentModel.DataAnnotations;

namespace GamingStore.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product Name is required")]
        [StringLength(150)]
        public string Name { get; set; }

        [Required]
        [Range(0.01, 10000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public ProductCategory Category { get; set; }

        [Required]
        [Range(0, 1000)]
        public int Stock { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsOnSale { get; set; }

        [Range(0, 100)]
        public int? DiscountPercentage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductComment> Comments { get; set; } = new List<ProductComment>();
        public ICollection<ProductRating> Ratings { get; set; } = new List<ProductRating>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
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
        [Display(Name = "Mice")]
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
