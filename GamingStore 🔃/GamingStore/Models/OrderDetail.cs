using System.ComponentModel.DataAnnotations;

namespace GamingStore.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }  // Foreign Key to Order
        public Order Order { get; set; }

        [Required]
        public int ProductId { get; set; }  // Foreign Key to Product
        public Product Product { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Quantity must be between 1 and 10")]
        public int Quantity { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Price must be between 1 and 10,000")]
        public decimal Price { get; set; }
    }

}
