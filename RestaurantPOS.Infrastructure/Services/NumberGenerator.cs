using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Infrastructure.Services;

/// <summary>
/// Generates sequential, branch-specific reference numbers (orders, invoices, shifts, POs).
/// Uses database sequences to guarantee uniqueness under concurrency.
/// </summary>
public class NumberGenerator : INumberGenerator
{
    private readonly ApplicationDbContext _context;

    public NumberGenerator(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateOrderNumberAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Orders
            .CountAsync(o => o.BranchId == branchId && o.CreatedDate.Date == DateTime.UtcNow.Date, cancellationToken);
        return $"ORD-{today}-{(count + 1):D4}";
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Invoices
            .CountAsync(i => i.BranchId == branchId && i.CreatedDate.Date == DateTime.UtcNow.Date, cancellationToken);
        return $"INV-{today}-{(count + 1):D4}";
    }

    public async Task<string> GeneratePurchaseOrderNumberAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.PurchaseOrders
            .CountAsync(p => p.BranchId == branchId && p.CreatedDate.Date == DateTime.UtcNow.Date, cancellationToken);
        return $"PO-{today}-{(count + 1):D4}";
    }

    public async Task<string> GenerateShiftNumberAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Shifts
            .CountAsync(s => s.BranchId == branchId && s.CreatedDate.Date == DateTime.UtcNow.Date, cancellationToken);
        return $"SHF-{today}-{(count + 1):D3}";
    }
}
