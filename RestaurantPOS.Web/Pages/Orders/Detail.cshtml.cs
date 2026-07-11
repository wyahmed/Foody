using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Web.Pages.Orders;

[Authorize]
public class DetailModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public DetailModel(ApplicationDbContext db) => _db = db;

    public OrderDetailDto? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderDetailDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderType = o.OrderType.ToString(),
                Status = o.Status.ToString(),
                TableName = o.Table != null ? o.Table.Name : null,
                CustomerName = o.Customer != null ? o.Customer.FirstName + (o.Customer.LastName != null ? " " + o.Customer.LastName : "") : null,
                CashierName = null,
                Notes = o.Notes,
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TaxAmount = o.TaxAmount,
                TotalAmount = o.TotalAmount,
                CreatedDate = o.CreatedDate,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice,
                    Notes = i.Notes,
                    Modifiers = i.Modifiers.Select(m => m.ModifierName).ToList()
                }).ToList(),
                Payments = o.Payments.Select(p => new PaymentDto
                {
                    Method = p.Method.ToString(),
                    Amount = p.Amount
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (Order == null) return NotFound();
        return Page();
    }
}

public record OrderDetailDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = "";
    public string OrderType { get; init; } = "";
    public string Status { get; init; } = "";
    public string? TableName { get; init; }
    public string? CustomerName { get; init; }
    public string? CashierName { get; init; }
    public string? Notes { get; init; }
    public decimal SubTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedDate { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public List<PaymentDto> Payments { get; init; } = new();
}

public record OrderItemDto
{
    public string ProductName { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public string? Notes { get; init; }
    public List<string> Modifiers { get; init; } = new();
}

public record PaymentDto
{
    public string Method { get; init; } = "";
    public decimal Amount { get; init; }
}
