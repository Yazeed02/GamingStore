using GamingStore.Data;
using GamingStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GamingStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<User> userManager, ApplicationDbContext db, IWebHostEnvironment webHostEnvironment, RoleManager<IdentityRole> roleManager)
        {
            this.db = db;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "No Role";
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // GET: Admin/EditUser/{id}
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            var currentRole = userRoles.FirstOrDefault(); // Only one role allowed

            ViewBag.Roles = allRoles;
            ViewBag.UserRole = currentRole;

            return View(user);
        }

        // POST: Admin/EditUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, User model, string selectedRole)
        {
            if (id != model.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound();

                // Update user properties
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email; // Ensure username matches email

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    AddErrors(updateResult);
                    return await ReloadEditUserView(user);
                }

                // Update user role
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.FirstOrDefault() != selectedRole)
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!string.IsNullOrEmpty(selectedRole))
                    {
                        await _userManager.AddToRoleAsync(user, selectedRole);
                    }
                }

                return RedirectToAction(nameof(ManageUsers));
            }

            return await ReloadEditUserView(model);
        }

        private async Task<IActionResult> ReloadEditUserView(User user)
        {
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            var currentRole = userRoles.FirstOrDefault();

            ViewBag.Roles = allRoles;
            ViewBag.UserRole = currentRole;

            return View(user);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(ManageUsers));
        }

        public async Task<IActionResult> ManageProducts(string searchString, ProductCategory? category, bool? featured, bool? onSale)
        {
            var products = db.Products.Include(p => p.Images).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) || p.Description.Contains(searchString));
            }

            if (category != null)
            {
                products = products.Where(p => p.Category == category);
            }

            if (featured.HasValue)
            {
                products = products.Where(p => p.IsFeatured == featured);
            }

            if (onSale.HasValue)
            {
                products = products.Where(p => p.IsOnSale == onSale);
            }

            ViewBag.Categories = Enum.GetValues<ProductCategory>();
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentCategory = category;
            ViewBag.IsFeatured = featured;
            ViewBag.IsOnSale = onSale;

            return View(await products.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.Name).ToListAsync());
        }

        public IActionResult AddProduct()
        {
            ViewBag.Categories = Enum.GetValues(typeof(ProductCategory)).Cast<ProductCategory>();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product product, List<IFormFile> imageFiles)
        {
            if (ModelState.IsValid)
            {
                product.CreatedDate = DateTime.UtcNow;
                product.LastUpdated = DateTime.UtcNow;

                db.Products.Add(product);
                await db.SaveChangesAsync();

                if (imageFiles != null && imageFiles.Count > 0)
                {
                    foreach (var imageFile in imageFiles)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", fileName);

                        using (var stream = new FileStream(uploadPath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        var image = new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/img/" + fileName
                        };

                        db.ProductImages.Add(image);
                    }

                    await db.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"{product.Name} added successfully!";
                return RedirectToAction(nameof(ManageProducts));
            }

            ViewBag.Categories = Enum.GetValues(typeof(ProductCategory)).Cast<ProductCategory>();
            return View(product);
        }

        // GET: Admin/EditProduct/5
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await db.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = Enum.GetValues(typeof(ProductCategory)).Cast<ProductCategory>();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product, List<IFormFile> imageFiles, int featuredImageId)
        {
            if (id != product.Id) return NotFound();

            var existing = await db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null) return NotFound();

            if (product.IsOnSale && !product.DiscountPercentage.HasValue)
            {
                ModelState.AddModelError("DiscountPercentage", "Discount is required when product is on sale.");
                return View(product);
            }

            if (ModelState.IsValid)
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.Category = product.Category;
                existing.Stock = product.Stock;
                existing.IsFeatured = product.IsFeatured;
                existing.IsOnSale = product.IsOnSale;
                existing.DiscountPercentage = product.IsOnSale ? product.DiscountPercentage : null;
                existing.LastUpdated = DateTime.UtcNow;

                if (imageFiles != null && imageFiles.Any())
                {
                    foreach (var file in imageFiles)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                            var path = Path.Combine(_webHostEnvironment.WebRootPath, "img", fileName);
                            using var stream = new FileStream(path, FileMode.Create);
                            await file.CopyToAsync(stream);

                            db.ProductImages.Add(new ProductImage
                            {
                                ProductId = existing.Id,
                                ImageUrl = "/img/" + fileName
                            });
                        }
                    }
                    await db.SaveChangesAsync();
                }

                var images = await db.ProductImages.Where(i => i.ProductId == id).ToListAsync();
                var featured = images.FirstOrDefault(i => i.Id == featuredImageId);
                if (featured != null)
                {
                    images.Remove(featured);
                    images.Insert(0, featured);
                    existing.Images = images;
                }

                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product updated!";
                return RedirectToAction("ManageProducts");
            }

            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductImage(int imageId)
        {
            var image = await db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId);
            if (image == null) return NotFound();

            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            db.ProductImages.Remove(image);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Image deleted successfully!";
            return RedirectToAction(nameof(EditProduct), new { id = image.ProductId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            foreach (var image in product.Images)
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            db.ProductImages.RemoveRange(product.Images);
            db.Products.Remove(product);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{product.Name} deleted successfully!";
            return RedirectToAction(nameof(ManageProducts));
        }

        [HttpGet]
        public async Task<IActionResult> ContactMessages(string sortBy = "Date", string sortOrder = "desc")
        {
            var messagesQuery = db.ContactMessages.AsQueryable();

            switch (sortBy.ToLower())
            {
                case "name":
                    messagesQuery = sortOrder == "asc"
                        ? messagesQuery.OrderBy(m => m.Name)
                        : messagesQuery.OrderByDescending(m => m.Name);
                    break;
                case "type":
                    messagesQuery = sortOrder == "asc"
                        ? messagesQuery.OrderBy(m => m.Type)
                        : messagesQuery.OrderByDescending(m => m.Type);
                    break;
                case "date":
                default:
                    messagesQuery = sortOrder == "asc"
                        ? messagesQuery.OrderBy(m => m.SubmittedAt)
                        : messagesQuery.OrderByDescending(m => m.SubmittedAt);
                    break;
            }

            var messages = await messagesQuery.ToListAsync();
            return View(messages);
        }


        private bool ProductExists(int id) => db.Products.Any(e => e.Id == id);
    }
}
