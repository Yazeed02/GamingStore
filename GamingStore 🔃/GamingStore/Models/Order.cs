using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace GamingStore.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }  // Foreign Key to User
        public User User { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(Pending|Processing|Completed)$", ErrorMessage = "Invalid status")]
        public string Status { get; set; }  // "Pending", "Processing", "Completed"

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}