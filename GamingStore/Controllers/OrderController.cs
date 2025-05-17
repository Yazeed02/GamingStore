using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using GamingStore.Models;
using Microsoft.EntityFrameworkCore;
using GamingStore.Data;
using Microsoft.AspNetCore.Authorization;

public class OrderController : Controller
{
    private readonly ApplicationDbContext db;
    private readonly UserManager<User> userManager;

    public OrderController(ApplicationDbContext db, UserManager<User> userManager)
    {
        this.db = db;
        this.userManager = userManager;
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var order = await db.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

        if (order == null) return NotFound();

        return View(order);
    }
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        var orders = await db.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    [Authorize]
    public async Task<IActionResult> Details(int id)
    {
        var order = await db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        var isAuthorized = user.Id == order.UserId ||
                          await userManager.IsInRoleAsync(user, "Admin") ||
                          await userManager.IsInRoleAsync(user, "Manager");

        if (!isAuthorized) return Forbid();

        return View(order);
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Manage()
    {
        var orders = await db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var order = await db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        db.Update(order);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Manage));
    }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await db.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        var isAuthorized = user.Id == order.UserId ||
                          await userManager.IsInRoleAsync(user, "Admin") ||
                          await userManager.IsInRoleAsync(user, "Manager");

        if (!isAuthorized) return Forbid();

        if (order.Status != "Pending")
        {
            TempData["ErrorMessage"] = "Order cannot be cancelled as it's no longer pending.";
            return RedirectToAction(nameof(Details), new { id });
        }

        order.Status = "Cancelled";
        db.Update(order);
        await db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Order has been cancelled successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }
}