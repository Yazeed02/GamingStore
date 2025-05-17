using GamingStore.Data;
using GamingStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    AddErrors(updateResult);
                    return await ReloadEditUserView(user);
                }

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
        public async Task<IActionResult> AddProduct(Product product, List<IFormFile> imageFiles, int featuredIndex = 0)
        {
            if (ModelState.IsValid)
            {
                if (product.IsOnSale && !product.DiscountPercentage.HasValue)
                {
                    ModelState.AddModelError(nameof(product.DiscountPercentage), "Discount is required when product is on sale.");
                }

                if (imageFiles == null || imageFiles.Count == 0)
                {
                    ModelState.AddModelError("imageFiles", "At least one image is required.");
                }

                if (!ModelState.IsValid)
                {
                    return View(product);
                }

                product.CreatedDate = DateTime.UtcNow;
                product.LastUpdated = DateTime.UtcNow;

                db.Products.Add(product);
                await db.SaveChangesAsync();

                var orderedFiles = imageFiles.ToList();
                if (featuredIndex >= 0 && featuredIndex < imageFiles.Count)
                {
                    var featured = orderedFiles[featuredIndex];
                    orderedFiles.RemoveAt(featuredIndex);
                    orderedFiles.Insert(0, featured); // Put featured first
                }

                foreach (var imageFile in orderedFiles)
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

                TempData["SuccessMessage"] = $"{product.Name} added successfully!";
                return RedirectToAction(nameof(ManageProducts));
            }

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

            var existingProduct = await db.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProduct == null) return NotFound();

            if (product.IsOnSale && !product.DiscountPercentage.HasValue)
            {
                ModelState.AddModelError("DiscountPercentage", "Discount is required when product is on sale.");
                return View(product);
            }

            if (ModelState.IsValid)
            {
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.Category = product.Category;
                existingProduct.Stock = product.Stock;
                existingProduct.IsFeatured = product.IsFeatured;
                existingProduct.IsOnSale = product.IsOnSale;
                existingProduct.DiscountPercentage = product.IsOnSale ? product.DiscountPercentage : null;
                existingProduct.LastUpdated = DateTime.UtcNow;

                List<ProductImage> newImages = new List<ProductImage>();
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    foreach (var file in imageFiles)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                            var path = Path.Combine(_webHostEnvironment.WebRootPath, "img", fileName);
                            using var stream = new FileStream(path, FileMode.Create);
                            await file.CopyToAsync(stream);

                            var newImage = new ProductImage
                            {
                                ProductId = existingProduct.Id,
                                ImageUrl = "/img/" + fileName
                            };
                            db.ProductImages.Add(newImage);
                            newImages.Add(newImage);
                        }
                    }
                    await db.SaveChangesAsync();
                }

                var allImages = await db.ProductImages
                    .Where(i => i.ProductId == existingProduct.Id)
                    .ToListAsync();

                if (featuredImageId != 0)
                {
                    ProductImage selectedImage = null;

                    if (featuredImageId < 0)
                    {
                        var tempIndex = Math.Abs(featuredImageId) - 1;
                        var orderedNewImages = newImages.OrderByDescending(i => i.Id).ToList();

                        if (tempIndex < orderedNewImages.Count)
                        {
                            selectedImage = orderedNewImages[tempIndex];
                        }
                    }
                    else
                    {
                        selectedImage = allImages.FirstOrDefault(i => i.Id == featuredImageId);
                    }

                    if (selectedImage != null)
                    {
                        allImages.Insert(0, selectedImage);
                    }
                }


                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction("ManageProducts");
            }

            ViewBag.Categories = Enum.GetValues(typeof(ProductCategory)).Cast<ProductCategory>();
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
            var product = await db.Products
                .Include(p => p.Images)
                .Include(p => p.CartItems)
                .Include(p => p.Favorites)
                .Include(p => p.OrderDetails)
                .Include(p => p.Comments)
                .Include(p => p.Ratings)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            try
            {
                foreach (var image in product.Images)
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                db.ProductImages.RemoveRange(product.Images);
                db.CartItems.RemoveRange(product.CartItems);
                db.Favorites.RemoveRange(product.Favorites);
                db.ProductComments.RemoveRange(product.Comments);
                db.ProductRatings.RemoveRange(product.Ratings);

                foreach (var orderDetail in product.OrderDetails)
                {
                    orderDetail.ProductId = null;
                    db.Update(orderDetail);
                }

                db.Products.Remove(product);
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{product.Name} deleted successfully!";
                return RedirectToAction(nameof(ManageProducts));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting product: {ex.Message}";
                return RedirectToAction(nameof(ManageProducts));
            }
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
