using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using GamingStore.Models;
using Microsoft.EntityFrameworkCore;
using GamingStore.Data;

public class CartController : Controller
{
    private readonly ApplicationDbContext db;
    private readonly UserManager<User> userManager;

    public CartController(ApplicationDbContext db, UserManager<User> userManager)
    {
        this.db = db;
        this.userManager = userManager;
    }
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var cartItems = await db.CartItems
            .Include(c => c.Product)
            .ThenInclude(p => p.Images)
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        return View(cartItems);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Json(new { success = false, message = "Please login to add items to cart" });

        var product = await db.Products.FindAsync(productId);
        if (product == null) return Json(new { success = false, message = "Product not found" });

        if (product.Stock < quantity)
            return Json(new { success = false, message = "Not enough stock available" });

        var existingItem = await db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            if (existingItem.Quantity > 10) existingItem.Quantity = 10;
        }
        else
        {
            db.CartItems.Add(new CartItem
            {
                UserId = user.Id,
                ProductId = productId,
                Quantity = quantity
            });
        }

        await db.SaveChangesAsync();

        // Return updated cart count
        var cartCount = await db.CartItems
            .Where(c => c.UserId == user.Id)
            .SumAsync(c => c.Quantity);

        return Json(new { success = true, cartCount });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
    {
        var item = await db.CartItems.FindAsync(cartItemId);
        if (item == null) return NotFound();

        var product = await db.Products.FindAsync(item.ProductId);
        if (product == null) return NotFound();

        if (quantity > product.Stock)
            return Json(new { success = false, message = "Not enough stock available" });

        item.Quantity = quantity;
        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int cartItemId)
    {
        var item = await db.CartItems.FindAsync(cartItemId);
        if (item == null) return NotFound();

        db.CartItems.Remove(item);
        await db.SaveChangesAsync();

        return Json(new { success = true });
    }
    [HttpGet]
    public async Task<IActionResult> GetCartCount()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Content("0");

        var count = await db.CartItems
            .Where(c => c.UserId == user.Id)
            .SumAsync(c => c.Quantity);

        return Content(count.ToString());
    }
}