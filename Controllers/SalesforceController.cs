using System.Security.Claims;
using Inventory_Management_Requirements.Models;
using Inventory_Management_Requirements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_Requirements.Controllers
{
    [Authorize]
    public class SalesforceController : Controller
    {
        private readonly SalesforceService _salesforceService;
        private readonly UserManager<ApplicationUser> _userManager;

        public SalesforceController(SalesforceService salesforceService, UserManager<ApplicationUser> userManager)
        {
            _salesforceService = salesforceService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Sync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new SalesforceSyncViewModel
            {
                Email = user.Email,
                FirstName = user.UserName.Split(' ').FirstOrDefault() ?? "",
                LastName = user.UserName.Split(' ').LastOrDefault() ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sync(SalesforceSyncViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Authenticate with Salesforce
            if (!await _salesforceService.AuthenticateAsync())
            {
                ModelState.AddModelError("", "Failed to authenticate with Salesforce.");
                return View(model);
            }

            // Create Account
            var accountName = $"{model.FirstName} {model.LastName}";
            var accountId = await _salesforceService.CreateAccountAsync(accountName);
            if (string.IsNullOrEmpty(accountId))
            {
                ModelState.AddModelError("", "Failed to create Account in Salesforce.");
                return View(model);
            }

            // Create Contact
            var success = await _salesforceService.CreateContactAsync(accountId, model.FirstName, model.LastName, model.Email, model.Phone, model.Street, model.City, model.State, model.PostalCode, model.Country);
            if (!success)
            {
                ModelState.AddModelError("", "Failed to create Contact in Salesforce.");
                return View(model);
            }

            TempData["Success"] = "Successfully synced user to Salesforce!";
            return RedirectToAction("Index", "Home");
        }
    }
}
