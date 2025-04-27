using Microsoft.AspNetCore.Mvc;
using GamingStore.Models;
using GamingStore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using GamingStore.Extensions;

namespace GamingStore.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext db, UserManager<User> userManager, ILogger<ProductController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchTerm, string category, string sortOrder)
        {
            try
            {
                var productsQuery = _db.Products
                    .Include(p => p.Images)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    productsQuery = productsQuery.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));

                if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ProductCategory>(category, out var parsedCategory))
                    productsQuery = productsQuery.Where(p => p.Category == parsedCategory);

                productsQuery = sortOrder switch
                {
                    "name" => productsQuery.OrderBy(p => p.Name),
                    "price" => productsQuery.OrderBy(p => p.Price),
                    "newest" => productsQuery.OrderByDescending(p => p.CreatedDate),
                    _ => productsQuery
                };

                var products = await productsQuery.ToListAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products with filtering");
                TempData["ErrorMessage"] = "An error occurred while retrieving products";
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Browse(string name)
        {
            var categoryGroups = new Dictionary<string, List<ProductCategory>>
            {
                ["Gaming"] = new() { ProductCategory.PlayStation, ProductCategory.Xbox, ProductCategory.Nintendo },
                ["PCs"] = new() { ProductCategory.GamingPCs, ProductCategory.GamingLaptops },
                ["Accessories"] = new() { ProductCategory.Keyboards, ProductCategory.Mice, ProductCategory.Headsets, ProductCategory.Controllers, ProductCategory.GamingAccessories },
                ["Others"] = new() { ProductCategory.GamingChairs, ProductCategory.GamingDesks, ProductCategory.Monitors, ProductCategory.VRHeadsets, ProductCategory.PCComponents, ProductCategory.GamingMerchandise }
            };

            List<Product> products;
            string displayName;

            if (categoryGroups.TryGetValue(name, out var categories))
            {
                products = _db.Products.Where(p => categories.Contains(p.Category)).ToList();
                displayName = name + " (All)";
            }
            else if (Enum.TryParse<ProductCategory>(name, out var singleCategory))
            {
                products = _db.Products.Where(p => p.Category == singleCategory).ToList();
                displayName = singleCategory.GetDisplayName() ?? name;
            }
            else
            {
                return NotFound();
            }

            ViewBag.CategoryName = displayName;
            return View(products);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid product ID");
            }

            try
            {
                var product = await _db.Products
                    .Include(p => p.Images)
                    .Include(p => p.Comments)
                        .ThenInclude(c => c.User)
                    .Include(p => p.Ratings)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found", id);
                    return NotFound();
                }

                // Calculate average rating safely
                ViewBag.AverageRating = product.Ratings?.Any() == true ?
                    product.Ratings.Average(r => r.Value) : 0;

                // Check if current user has favorited this product
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        ViewBag.IsFavorited = await _db.Favorites
                            .AnyAsync(f => f.ProductId == id && f.UserId == userId);
                    }
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product details for ID {ProductId}", id);
                TempData["ErrorMessage"] = "An error occurred while retrieving product details";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int productId, string content)
        {
            if (productId <= 0)
            {
                return BadRequest("Invalid product ID");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Comment cannot be empty";
                return RedirectToAction("Details", new { id = productId });
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unable to get user ID for comment submission");
                    return Challenge(); // Force re-authentication
                }

                var productExists = await _db.Products.AnyAsync(p => p.Id == productId);
                if (!productExists)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for comment", productId);
                    return NotFound();
                }

                var comment = new ProductComment
                {
                    ProductId = productId,
                    UserId = userId,
                    Content = content.Trim()
                };

                await _db.ProductComments.AddAsync(comment);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your review has been posted";
                return RedirectToAction("Details", new { id = productId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to product ID {ProductId}", productId);
                TempData["ErrorMessage"] = "An error occurred while submitting your review";
                return RedirectToAction("Details", new { id = productId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRating(int productId, int value)
        {
            if (productId <= 0)
            {
                return BadRequest("Invalid product ID");
            }

            if (value < 1 || value > 5)
            {
                TempData["ErrorMessage"] = "Please select a valid rating between 1 and 5";
                return RedirectToAction("Details", new { id = productId });
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unable to get user ID for rating submission");
                    return Challenge();
                }

                var productExists = await _db.Products.AnyAsync(p => p.Id == productId);
                if (!productExists)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for rating", productId);
                    return NotFound();
                }

                var existingRating = await _db.ProductRatings
                    .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

                if (existingRating != null)
                {
                    existingRating.Value = value;
                    existingRating.CreatedAt = DateTime.UtcNow;
                    TempData["SuccessMessage"] = "Your rating has been updated";
                }
                else
                {
                    var rating = new ProductRating
                    {
                        ProductId = productId,
                        UserId = userId,
                        Value = value
                    };
                    await _db.ProductRatings.AddAsync(rating);
                    TempData["SuccessMessage"] = "Thank you for rating this product";
                }

                await _db.SaveChangesAsync();
                return RedirectToAction("Details", new { id = productId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding rating to product ID {ProductId}", productId);
                TempData["ErrorMessage"] = "An error occurred while submitting your rating";
                return RedirectToAction("Details", new { id = productId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            if (productId <= 0)
            {
                return BadRequest("Invalid product ID");
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unable to get user ID for favorite toggle");
                    return Challenge();
                }

                var productExists = await _db.Products.AnyAsync(p => p.Id == productId);
                if (!productExists)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for favorite toggle", productId);
                    return NotFound();
                }

                var existingFavorite = await _db.Favorites
                    .FirstOrDefaultAsync(f => f.ProductId == productId && f.UserId == userId);

                if (existingFavorite != null)
                {
                    _db.Favorites.Remove(existingFavorite);
                    TempData["SuccessMessage"] = "Removed from favorites";
                }
                else
                {
                    var favorite = new Favorite
                    {
                        ProductId = productId,
                        UserId = userId
                    };
                    await _db.Favorites.AddAsync(favorite);
                    TempData["SuccessMessage"] = "Added to favorites";
                }

                await _db.SaveChangesAsync();
                return RedirectToAction("Details", new { id = productId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for product ID {ProductId}", productId);
                TempData["ErrorMessage"] = "An error occurred while updating favorites";
                return RedirectToAction("Details", new { id = productId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid comment ID");
            }

            try
            {
                var comment = await _db.ProductComments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                {
                    _logger.LogWarning("Comment with ID {CommentId} not found", id);
                    return NotFound();
                }

                var currentUserId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _logger.LogWarning("Unable to get user ID for comment deletion");
                    return Challenge();
                }

                // Only allow the comment owner or admin to delete
                if (comment.UserId != currentUserId && !User.IsInRole("Admin"))
                {
                    _logger.LogWarning("User {UserId} attempted to delete comment {CommentId} they don't own",
                        currentUserId, id);
                    return Forbid();
                }

                _db.ProductComments.Remove(comment);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Comment deleted successfully";
                return RedirectToAction("Details", new { id = comment.ProductId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment with ID {CommentId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the comment";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        return value.GetType()
                  .GetMember(value.ToString())[0]
                  .GetCustomAttributes(typeof(DisplayAttribute), false)
                  .Cast<DisplayAttribute>()
                  .FirstOrDefault()?.Name ?? value.ToString();
    }
}