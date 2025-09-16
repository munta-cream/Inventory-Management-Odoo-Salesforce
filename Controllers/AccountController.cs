using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Inventory_Management_Requirements.Models;

namespace Inventory_Management_Requirements.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Only allow the user themselves or admins to access
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && user.Id != userId)
            {
                return Forbid();
            }

            return View(user);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GenerateApiToken()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.ApiToken = Guid.NewGuid().ToString();
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to generate API token.");
                return View("Profile", user);
            }

            return RedirectToAction("Profile");
        }
    }
}
