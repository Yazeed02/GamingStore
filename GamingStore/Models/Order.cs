using GamingStore.Models;
using System.ComponentModel.DataAnnotations;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "Cash on Delivery";

    [Required]
    public string UserId { get; set; }
    public User User { get; set; }

    [Required]
    public DateTime OrderDate { get; set; }

    [Required]
    [RegularExpression("^(Pending|Processing|Completed|Cancelled)$")]
    public string Status { get; set; }

    // Shipping Information
    public string FullName { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string PhoneNumber { get; set; }

    // Order Totals
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }

    public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}