using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;
using System.Text.Json;

namespace Inventory_Management_Requirements.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("aggregated-data")]
        public async Task<IActionResult> GetAggregatedData()
        {
            var token = Request.Headers["X-API-Token"].FirstOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("API token required");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ApiToken == token);
            if (user == null)
            {
                return Unauthorized("Invalid API token");
            }

            var inventories = await _context.Inventories
                .Where(i => i.CreatedById == user.Id)
                .Include(i => i.Fields)
                .Include(i => i.Items)
                .ToListAsync();

            var aggregatedData = new List<object>();

            foreach (var inventory in inventories)
            {
                var inventoryData = new
                {
                    inventoryId = inventory.Id,
                    title = inventory.Title,
                    fields = inventory.Fields.Select(f => new
                    {
                        fieldId = f.Id,
                        title = f.Title,
                        type = f.Type.ToString(),
                        aggregation = AggregateFieldData(f, inventory.Items)
                    }).ToList()
                };
                aggregatedData.Add(inventoryData);
            }

            return Ok(aggregatedData);
        }

        private object AggregateFieldData(CustomField field, ICollection<Item> items)
        {
            var fieldValues = items
                .Where(i => !string.IsNullOrEmpty(i.FieldData))
                .Select(i =>
                {
                    try
                    {
                        var json = JsonDocument.Parse(i.FieldData);
                        if (json.RootElement.TryGetProperty(field.Title, out var value))
                        {
                            return value;
                        }
                    }
                    catch { }
                    return (JsonElement?)null;
                })
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            switch (field.Type)
            {
                case FieldType.Number:
                    var numbers = fieldValues
                        .Where(v => v.ValueKind == JsonValueKind.Number)
                        .Select(v => v.GetDecimal())
                        .ToList();
                    return new
                    {
                        count = numbers.Count,
                        average = numbers.Any() ? numbers.Average() : (decimal?)null,
                        min = numbers.Any() ? numbers.Min() : (decimal?)null,
                        max = numbers.Any() ? numbers.Max() : (decimal?)null
                    };

                case FieldType.Boolean:
                    var booleans = fieldValues
                        .Where(v => v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False)
                        .Select(v => v.GetBoolean())
                        .ToList();
                    return new
                    {
                        count = booleans.Count,
                        trueCount = booleans.Count(b => b),
                        falseCount = booleans.Count(b => !b),
                        percentageTrue = booleans.Any() ? (double)booleans.Count(b => b) / booleans.Count * 100 : 0
                    };

                case FieldType.SingleLineText:
                case FieldType.MultiLineText:
                    var texts = fieldValues
                        .Where(v => v.ValueKind == JsonValueKind.String)
                        .Select(v => v.GetString())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    var grouped = texts.GroupBy(t => t).OrderByDescending(g => g.Count()).FirstOrDefault();
                    return new
                    {
                        count = texts.Count,
                        popularAnswer = grouped?.Key,
                        popularCount = grouped?.Count() ?? 0
                    };

                default:
                    return new { count = fieldValues.Count };
            }
        }
    }
}
