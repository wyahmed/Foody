using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Shared.Models;

namespace RestaurantPOS.API.Controllers;

/// <summary>Customer management API.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CustomersController(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Get paginated customers.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        var tenantId = _currentUser.TenantId;
        var q = _db.Customers.AsNoTracking().Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.FirstName.Contains(search) ||
                (c.LastName != null && c.LastName.Contains(search)) ||
                (c.Phone != null && c.Phone.Contains(search)));

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(c => c.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                Name = c.FirstName + (c.LastName != null ? " " + c.LastName : ""),
                c.Phone,
                c.Email,
                c.LoyaltyPoints,
                c.TotalSpent,
                c.TotalOrders
            })
            .ToListAsync();

        return Ok(PagedResult<object>.Create(items.Cast<object>().ToList().AsReadOnly(), total, page, pageSize));
    }

    /// <summary>Get customer by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var tenantId = _currentUser.TenantId;
        var customer = await _db.Customers
            .AsNoTracking()
            .Where(c => c.Id == id && c.TenantId == tenantId)
            .Select(c => new
            {
                c.Id,
                Name = c.FirstName + (c.LastName != null ? " " + c.LastName : ""),
                c.FirstName,
                c.LastName,
                c.Phone,
                c.Email,
                c.LoyaltyPoints,
                c.TotalSpent,
                c.TotalOrders,
                c.Notes
            })
            .FirstOrDefaultAsync();

        if (customer == null) return NotFound();
        return Ok(customer);
    }

    /// <summary>Create a new customer.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            TenantId = _currentUser.TenantId ?? Guid.Empty,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Notes = request.Notes
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new { customer.Id });
    }

    /// <summary>Add loyalty points to a customer.</summary>
    [HttpPost("{id:guid}/loyalty")]
    public async Task<IActionResult> AddLoyaltyPoints(Guid id, [FromBody] LoyaltyRequest request)
    {
        var customer = await _db.Customers
            .Where(c => c.Id == id && c.TenantId == _currentUser.TenantId)
            .FirstOrDefaultAsync();

        if (customer == null) return NotFound();

        customer.LoyaltyPoints += request.Points;
        if (customer.LoyaltyPoints < 0) customer.LoyaltyPoints = 0;
        await _db.SaveChangesAsync();

        return Ok(new { customer.LoyaltyPoints });
    }
}

public record CreateCustomerRequest(string FirstName, string? LastName, string? Phone, string? Email, string? Notes);
public record LoyaltyRequest(int Points);
