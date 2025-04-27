using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GamingStore.Models
{
    public class User : IdentityUser
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
        public string FullName { get; set; }

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public List<Favorite> Favorites { get; set; } = new List<Favorite>();
        public List<Order> Orders { get; set; } = new List<Order>();
    }
}
