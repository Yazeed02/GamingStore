using System.ComponentModel.DataAnnotations;

namespace GamingStore.Models
{
    public class Favorite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }  
        public User User { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}