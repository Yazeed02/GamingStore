using GamingStore.Data;
using GamingStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamingStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db;
        public HomeController(ApplicationDbContext db)
        {
            this.db = db;
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
                return RedirectToAction("Index", "Home"); // Redirect to homepage
            }

            return View(model);
        }
    }
}
