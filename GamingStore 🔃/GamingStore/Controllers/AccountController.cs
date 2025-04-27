using GamingStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GamingStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginModel());
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["AlertMessage"] = "Please provide valid login credentials.";
                TempData["AlertType"] = "error";
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["AlertMessage"] = "User not found.";
                TempData["AlertType"] = "error";
                return View(model);
            }

            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordCorrect)
            {
                TempData["AlertMessage"] = "Invalid login attempt.";
                TempData["AlertType"] = "error";
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                TempData["AlertMessage"] = "You are now logged in!";
                TempData["AlertType"] = "success";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["AlertMessage"] = "Invalid login attempt.";
                TempData["AlertType"] = "error";
                return View(model);
            }
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            var roles = new List<SelectListItem>
            {
                new SelectListItem { Text = "Buyer", Value = "Buyer" },
                new SelectListItem { Text = "Admin", Value = "Admin" }
            };
            ViewBag.Roles = roles;
            return View(new RegisterModel());
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User { UserName = model.Email, Email = model.Email, FullName = model.FullName };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(model.Role));
                    }

                    await _userManager.AddToRoleAsync(user, model.Role);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["AlertMessage"] = "Registration successful, you are now logged in.";
                    TempData["AlertType"] = "success";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            TempData["AlertMessage"] = "Please fix the errors below.";
            TempData["AlertType"] = "error";
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["AlertMessage"] = "You have been logged out successfully.";
            TempData["AlertType"] = "success";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            if (string.IsNullOrEmpty(role))
            {
                role = "User";
            }

            var model = new UserProfileModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Role = role
            };

            return View(model);
        }
        

        // GET: /Account/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new EditProfileModel
            {
                FullName = user.FullName,
                Email = user.Email
            };

            return View(model);
        }

        // POST: /Account/EditProfile
        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                // Update basic profile information
                user.FullName = model.FullName;
                user.Email = model.Email;

                // Update the password if provided
                if (!string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
                {
                    var passwordChangeResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                    if (!passwordChangeResult.Succeeded)
                    {
                        foreach (var error in passwordChangeResult.Errors)
                        {
                            if (error.Code == "PasswordMismatch")
                            {
                                TempData["AlertMessage"] = "The current password is incorrect.";
                                TempData["AlertType"] = "error";
                            }
                            else
                            {
                                TempData["AlertMessage"] = "The new password is invalid. Please ensure it meets the requirements.";
                                TempData["AlertType"] = "error";
                            }
                        }
                        return View(model);
                    }
                }

                // Save changes to the user
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["AlertMessage"] = "Profile updated successfully.";
                    TempData["AlertType"] = "success";
                    return RedirectToAction("Profile");
                }
                else
                {
                    TempData["AlertMessage"] = "An error occurred while updating the profile.";
                    TempData["AlertType"] = "error";
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found.");

            await _signInManager.SignOutAsync();
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return StatusCode(500, "Error deleting account.");
            }

            return RedirectToAction("Index", "Home");
        }

    }
}
