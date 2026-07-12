using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Web.Pages.Orders;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ---- query params ----
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? OrderType { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? DateFrom { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? DateTo { get; set; }
    [BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;

    // ---- view model ----
    public List<OrderRowDto> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int From { get; set; }
    public int To { get; set; }

    private const int PageSize = 20;

    public async Task OnGetAsync()
    {
        var branchId = _currentUser.BranchId;

        var q = _db.Orders
            .AsNoTracking()
            .Where(o => o.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(o => o.OrderNumber.Contains(Search));

        if (!string.IsNullOrWhiteSpace(Status) && Enum.TryParse<OrderStatus>(Status, out var s))
            q = q.Where(o => o.Status == s);

        if (!string.IsNullOrWhiteSpace(OrderType) && Enum.TryParse<RestaurantPOS.Domain.Enums.OrderType>(OrderType, out var ot))
            q = q.Where(o => o.OrderType == ot);

        if (DateFrom.HasValue) q = q.Where(o => o.CreatedDate >= DateFrom.Value);
        if (DateTo.HasValue) q = q.Where(o => o.CreatedDate <= DateTo.Value.AddDays(1));

        TotalCount = await q.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        Page = Math.Max(1, Math.Min(Page, Math.Max(1, TotalPages)));
        From = (Page - 1) * PageSize + 1;
        To = Math.Min(Page * PageSize, TotalCount);

        Orders = await q
            .OrderByDescending(o => o.CreatedDate)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .Select(o => new OrderRowDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderType = o.OrderType.ToString(),
                Status = o.Status.ToString(),
                TableName = o.Table != null ? o.Table.Name : null,
                CustomerName = o.Customer != null ? o.Customer.FirstName + (o.Customer.LastName != null ? " " + o.Customer.LastName : "") : null,
                ItemCount = o.Items.Count,
                Total = o.TotalAmount,
                CreatedDate = o.CreatedDate
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        var branchId = _currentUser.BranchId;
        if (!branchId.HasValue) return Forbid();

        var order = await _db.Orders
            .Where(o => o.Id == id && o.BranchId == branchId.Value)
            .FirstOrDefaultAsync();

        if (order == null) return NotFound();
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            return BadRequest();

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Order cancelled successfully.";
        return RedirectToPage();
    }
}

public record OrderRowDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = "";
    public string OrderType { get; init; } = "";
    public string Status { get; init; } = "";
    public string? TableName { get; init; }
    public string? CustomerName { get; init; }
    public int ItemCount { get; init; }
    public decimal Total { get; init; }
    public DateTime CreatedDate { get; init; }
}
