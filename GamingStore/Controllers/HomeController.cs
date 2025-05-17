using GamingStore.Data;
using GamingStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamingStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<User> userManager;

        public HomeController(ApplicationDbContext db, UserManager<User> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }
        public IActionResult Index()
        {
            var products = db.Products
                             .Include(p => p.Images)
                             .ToList();

            var categories = Enum.GetValues(typeof(ProductCategory))
                                 .Cast<ProductCategory>()
                                 .ToList();

            ViewBag.Categories = categories;

            return View(products);
        }

        [HttpGet]
        public JsonResult LiveSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            var results = db.Products
                .Include(p => p.Images) 
                .Where(p => p.Name.ToLower().Contains(query.ToLower()))
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    imageUrl = p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault().ImageUrl : "/img/placeholder.png"
                })
                .Take(10)
                .ToList();

            return Json(results);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Contact(ContactMessage model)
        {
            if (ModelState.IsValid)
            {
                db.ContactMessages.Add(model);
                await db.SaveChangesAsync();

                TempData["MessageSent"] = "Thank you! Your message was sent successfully.";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var favorites = await db.Favorites
                .Include(f => f.Product)
                .ThenInclude(p => p.Images)
                .Where(f => f.UserId == user.Id)
                .ToListAsync();

            return View(favorites);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var existing = await db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == user.Id && f.ProductId == productId);

            if (existing != null)
            {
                db.Favorites.Remove(existing);
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Favorites"); 
        }
    }
}
