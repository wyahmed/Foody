using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Web.Pages.Reports;

[Authorize(Roles = "SuperAdmin,Admin,Manager,Accountant,Auditor")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public DateTime DateFrom { get; set; } = DateTime.Today.AddDays(-29);
    [BindProperty(SupportsGet = true)] public DateTime DateTo { get; set; } = DateTime.Today;

    public decimal TotalSales { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalVat { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrder { get; set; }

    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<SalesByTypeDto> SalesByType { get; set; } = new();
    public List<DailySalesDto> DailySales { get; set; } = new();

    public async Task OnGetAsync()
    {
        var branchId = _currentUser.BranchId;
        var from = DateFrom.Date;
        var to = DateTo.Date.AddDays(1);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.BranchId == branchId
                && o.Status == OrderStatus.Completed
                && o.CreatedDate >= from
                && o.CreatedDate < to)
            .Select(o => new
            {
                o.TotalAmount,
                o.SubTotal,
                o.DiscountAmount,
                o.TaxAmount,
                o.OrderType,
                Date = o.CreatedDate.Date
            })
            .ToListAsync();

        GrossSales = orders.Sum(o => o.SubTotal);
        TotalDiscount = orders.Sum(o => o.DiscountAmount);
        TotalVat = orders.Sum(o => o.TaxAmount);
        TotalSales = orders.Sum(o => o.TotalAmount);
        TotalOrders = orders.Count;
        AverageOrder = TotalOrders > 0 ? TotalSales / TotalOrders : 0;

        SalesByType = orders
            .GroupBy(o => o.OrderType.ToString())
            .Select(g => new SalesByTypeDto
            {
                OrderType = g.Key,
                Count = g.Count(),
                Total = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        DailySales = orders
            .GroupBy(o => o.Date)
            .Select(g => new DailySalesDto
            {
                Date = g.Key,
                OrderCount = g.Count(),
                GrossSales = g.Sum(o => o.SubTotal),
                Discount = g.Sum(o => o.DiscountAmount),
                Vat = g.Sum(o => o.TaxAmount),
                NetSales = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(x => x.Date)
            .ToList();

        TopProducts = await _db.OrderItems
            .AsNoTracking()
            .Where(i => i.Order.BranchId == branchId
                && i.Order.Status == OrderStatus.Completed
                && i.Order.CreatedDate >= from
                && i.Order.CreatedDate < to)
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductDto
            {
                ProductName = g.Key,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(10)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostExportExcelAsync(DateTime dateFrom, DateTime dateTo)
    {
        DateFrom = dateFrom;
        DateTo = dateTo;
        await OnGetAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sales Report");

        ws.Cell(1, 1).Value = $"Sales Report: {DateFrom:dd MMM yyyy} - {DateTo:dd MMM yyyy}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        int row = 3;
        ws.Cell(row, 1).Value = "Date"; ws.Cell(row, 2).Value = "Orders";
        ws.Cell(row, 3).Value = "Gross Sales"; ws.Cell(row, 4).Value = "Discount";
        ws.Cell(row, 5).Value = "VAT"; ws.Cell(row, 6).Value = "Net Sales";
        ws.Row(row).Style.Font.Bold = true;
        row++;

        foreach (var d in DailySales)
        {
            ws.Cell(row, 1).Value = d.Date.ToString("dd MMM yyyy");
            ws.Cell(row, 2).Value = d.OrderCount;
            ws.Cell(row, 3).Value = (double)d.GrossSales;
            ws.Cell(row, 4).Value = (double)d.Discount;
            ws.Cell(row, 5).Value = (double)d.Vat;
            ws.Cell(row, 6).Value = (double)d.NetSales;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"SalesReport_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.xlsx");
    }
}

public record TopProductDto { public string ProductName { get; init; } = ""; public decimal TotalQuantity { get; init; } public decimal TotalRevenue { get; init; } }
public record SalesByTypeDto { public string OrderType { get; init; } = ""; public int Count { get; init; } public decimal Total { get; init; } }
public record DailySalesDto { public DateTime Date { get; init; } public int OrderCount { get; init; } public decimal GrossSales { get; init; } public decimal Discount { get; init; } public decimal Vat { get; init; } public decimal NetSales { get; init; } }
