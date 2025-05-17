using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using GamingStore.Models;
using Microsoft.EntityFrameworkCore;
using GamingStore.Data;

public class CheckoutController : Controller
{
    private readonly ApplicationDbContext db;
    private readonly UserManager<User> userManager;

    public CheckoutController(ApplicationDbContext db, UserManager<User> userManager)
    {
        this.db = db;
        this.userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var cartItems = await db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

        return View(cartItems);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(string fullName, string address, string city, string phoneNumber)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var cartItems = await db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

        foreach (var item in cartItems)
        {
            if (item.Product.Stock < item.Quantity)
            {
                return Json(new { success = false, message = $"{item.Product.Name} doesn't have enough stock" });
            }
        }

        var subtotal = cartItems.Sum(item =>
            item.Product.IsOnSale && item.Product.DiscountPercentage.HasValue
                ? item.Product.Price * (100 - item.Product.DiscountPercentage.Value) / 100 * item.Quantity
                : item.Product.Price * item.Quantity);

        var tax = subtotal * 0.1m; 
        var deliveryFee = 3.00m; // $3 delivery fee
        var total = subtotal + tax + deliveryFee;

        var order = new Order
        {
            UserId = user.Id,
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
            PaymentMethod = "Cash on Delivery",
            FullName = fullName,
            Address = address,
            City = city,
            PhoneNumber = phoneNumber,
            Subtotal = subtotal,
            Tax = tax,
            DeliveryFee = deliveryFee,
            Total = total
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        foreach (var item in cartItems)
        {
            db.OrderDetails.Add(new OrderDetail
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Product.IsOnSale && item.Product.DiscountPercentage.HasValue ?
                    item.Product.Price * (100 - item.Product.DiscountPercentage.Value) / 100 :
                    item.Product.Price
            });

            item.Product.Stock -= item.Quantity;
        }

        db.CartItems.RemoveRange(cartItems);
        await db.SaveChangesAsync();

        return Json(new { success = true, orderId = order.Id });
    }
}