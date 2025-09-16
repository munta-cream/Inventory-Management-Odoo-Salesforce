using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;
using Inventory_Management_Requirements.Services;

public class ItemsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICustomIdGenerator _idGenerator;

    public ItemsController(ApplicationDbContext db, ICustomIdGenerator idGenerator)
    {
        _db = db;
        _idGenerator = idGenerator;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int inventoryId)
    {
        var inventory = _db.Inventories
            .Include(i => i.Fields)
            .FirstOrDefault(i => i.Id == inventoryId);

        if (inventory == null) return NotFound();

        // Check access control
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Forbid();
        var isAdmin = User.IsInRole("Admin");
        var hasAccess = await HasAccessToInventory(inventoryId, userId, isAdmin);
        if (!hasAccess) return Forbid();

        ViewBag.Inventory = inventory;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int inventoryId, Item item)
    {
        if (!User.Identity.IsAuthenticated) return Forbid();

        var inventory = await _db.Inventories
            .Include(i => i.IdFormat)
            .FirstOrDefaultAsync(i => i.Id == inventoryId);

        if (inventory == null) return NotFound();

        // Check access control
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Forbid();
        var isAdmin = User.IsInRole("Admin");
        var hasAccess = await HasAccessToInventory(inventoryId, userId, isAdmin);
        if (!hasAccess) return Forbid();

        // Generate custom ID
        var itemCount = await _db.Items.CountAsync(i => i.InventoryId == inventoryId);
        var formatJson = JsonDocument.Parse(inventory.IdFormat.FormatDefinition);
        item.CustomId = _idGenerator.Generate(inventoryId, formatJson, itemCount + 1);

        item.InventoryId = inventoryId;
        item.CreatedById = userId;
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        if (ModelState.IsValid)
        {
            _db.Items.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Details", "Inventories", new { id = inventoryId });
        }

        ViewBag.Inventory = inventory;
        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = _db.Items
            .Include(i => i.Inventory)
            .ThenInclude(i => i.Fields)
            .FirstOrDefault(i => i.Id == id);

        if (item == null) return NotFound();

        // Check access control
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        var hasAccess = await HasAccessToInventory(item.InventoryId, userId, isAdmin);
        if (!hasAccess) return Forbid();

        ViewBag.Inventory = item.Inventory;
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("Id,InventoryId,CreatedById,CustomId,FieldData,CreatedAt,UpdatedAt,Name,Description,Brand,Model,Quantity,Price,Weight,InStock,Fragile,Perishable")] Item item)
    {
        Console.WriteLine("=== EDIT ITEM POST STARTED ===");
        Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
        Console.WriteLine($"Item ID: {item?.Id}");
        Console.WriteLine($"Item Name: {item?.Name}");

        if (!User.Identity.IsAuthenticated)
        {
            Console.WriteLine("ERROR: User not authenticated");
            return Forbid();
        }

        var existingItem = await _db.Items
            .Include(i => i.Inventory)
            .FirstOrDefaultAsync(i => i.Id == item.Id);

        Console.WriteLine($"Existing item found: {existingItem != null}");
        if (existingItem != null)
        {
            Console.WriteLine($"Existing item name: {existingItem.Name}");
            Console.WriteLine($"Existing item inventory ID: {existingItem.InventoryId}");
        }

        if (existingItem == null)
        {
            Console.WriteLine("ERROR: Existing item not found");
            return NotFound();
        }

        // Check access control
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        var hasAccess = await HasAccessToInventory(existingItem.InventoryId, userId, isAdmin);
        Console.WriteLine($"User ID: {userId}");
        Console.WriteLine($"Is Admin: {isAdmin}");
        Console.WriteLine($"Has access: {hasAccess}");

        if (!hasAccess)
        {
            Console.WriteLine("ERROR: User does not have access");
            return Forbid();
        }

        // Update FieldData and also update individual properties in FieldData JSON
        Console.WriteLine("Updating item properties...");
        existingItem.Name = item.Name;
        existingItem.Description = item.Description;
        existingItem.Brand = item.Brand;
        existingItem.Model = item.Model;
        existingItem.Quantity = item.Quantity ?? 0;
        existingItem.Price = item.Price ?? 0;
        existingItem.Weight = item.Weight ?? 0;
        existingItem.InStock = item.InStock;
        existingItem.Fragile = item.Fragile;
        existingItem.Perishable = item.Perishable;

        existingItem.UpdatedAt = DateTime.UtcNow;

        Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            Console.WriteLine("ModelState errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        if (ModelState.IsValid)
        {
            Console.WriteLine("Saving changes to database...");
            _db.Items.Update(existingItem);
            var saveResult = await _db.SaveChangesAsync();
            Console.WriteLine($"SaveChanges result: {saveResult} rows affected");
            Console.WriteLine("=== EDIT ITEM POST COMPLETED SUCCESSFULLY ===");
            return RedirectToAction("Details", "Inventories", new { id = existingItem.InventoryId });
        }

        Console.WriteLine("ModelState is invalid, returning view");
        ViewBag.Inventory = existingItem.Inventory;
        return View(existingItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();

        // Check access control
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        var hasAccess = await HasAccessToInventory(item.InventoryId, userId, isAdmin);
        if (!hasAccess) return Forbid();

        var inventoryId = item.InventoryId;
        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Details", "Inventories", new { id = inventoryId });
    }

    private async Task<bool> HasAccessToInventory(int inventoryId, string? userId, bool isAdmin)
    {
        if (userId == null) return false;

        var inventory = await _db.Inventories.FindAsync(inventoryId);
        if (inventory == null) return false;

        
        return inventory.CreatedById == userId ||
               inventory.IsPublic ||
               isAdmin ||
               await _db.InventoryAccesses.AnyAsync(a => a.InventoryId == inventoryId && a.UserId == userId);
    }
}
