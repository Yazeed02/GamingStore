using System.ComponentModel.DataAnnotations;
namespace GamingStore.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }  // Foreign Key to User
        public User User { get; set; }

        [Required]
        public int ProductId { get; set; }  // Foreign Key to Product
        public Product Product { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Quantity must be between 1 and 10")]
        public int Quantity { get; set; }
    }
}