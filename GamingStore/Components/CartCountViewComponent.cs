using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using GamingStore.Models;
using Microsoft.EntityFrameworkCore;
using GamingStore.Data;

public class CartCountViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public CartCountViewComponent(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Content("0");

        var count = await _context.CartItems
            .Where(c => c.UserId == user.Id)
            .SumAsync(c => c.Quantity);

        return Content(count.ToString());
    }
}