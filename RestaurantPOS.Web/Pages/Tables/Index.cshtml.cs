using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Web.Pages.Tables;

[Authorize(Roles = "Admin,Manager,Cashier")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public List<SectionDto> Sections { get; set; } = new();

    public async Task OnGetAsync()
    {
        var branchId = _currentUser.BranchId;

        Sections = await _db.TableSections
            .AsNoTracking()
            .Where(s => s.BranchId == branchId)
            .OrderBy(s => s.SortOrder)
            .Select(s => new SectionDto
            {
                Name = s.Name,
                Tables = s.Tables
                    .OrderBy(t => t.Name)
                    .Select(t => new TableDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Capacity = t.Capacity,
                        Status = t.Status.ToString(),
                        CurrentOrderId = t.Orders
                            .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                            .Select(o => (Guid?)o.Id)
                            .FirstOrDefault(),
                        OrderNumber = t.Orders
                            .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                            .Select(o => o.OrderNumber)
                            .FirstOrDefault()
                    }).ToList()
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostMarkCleaningAsync(Guid tableId)
    {
        var branchId = _currentUser.BranchId;
        if (!branchId.HasValue) return Forbid();

        var table = await _db.DiningTables
            .Where(t => t.Id == tableId && t.BranchId == branchId.Value)
            .FirstOrDefaultAsync();

        if (table != null) { table.Status = TableStatus.Cleaning; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAvailableAsync(Guid tableId)
    {
        var branchId = _currentUser.BranchId;
        if (!branchId.HasValue) return Forbid();

        var table = await _db.DiningTables
            .Where(t => t.Id == tableId && t.BranchId == branchId.Value)
            .FirstOrDefaultAsync();

        if (table != null) { table.Status = TableStatus.Available; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}

public record SectionDto { public string Name { get; init; } = ""; public List<TableDto> Tables { get; init; } = new(); }
public record TableDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public int Capacity { get; init; }
    public string Status { get; init; } = "";
    public Guid? CurrentOrderId { get; init; }
    public string? OrderNumber { get; init; }
}
