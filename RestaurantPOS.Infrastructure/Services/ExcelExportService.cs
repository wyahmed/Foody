using ClosedXML.Excel;
using RestaurantPOS.Domain.Interfaces;
using System.Reflection;

namespace RestaurantPOS.Infrastructure.Services;

/// <summary>Excel export service using ClosedXML.</summary>
public class ExcelExportService : IExcelExportService
{
    public Task<byte[]> ExportAsync<T>(IEnumerable<T> data, string sheetName = "Sheet1", CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.PropertyType.IsPrimitive
                || p.PropertyType == typeof(string)
                || p.PropertyType == typeof(decimal)
                || p.PropertyType == typeof(DateTime)
                || p.PropertyType == typeof(DateTime?)
                || p.PropertyType == typeof(bool)
                || p.PropertyType == typeof(int)
                || p.PropertyType == typeof(Guid))
            .ToArray();

        // Header row
        for (var col = 0; col < properties.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = properties[col].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2d6a4f");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Data rows
        var rowIndex = 2;
        foreach (var item in data)
        {
            for (var col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(item);
                var cell = worksheet.Cell(rowIndex, col + 1);
                if (value is DateTime dt) cell.Value = dt.ToString("yyyy-MM-dd HH:mm:ss");
                else if (value is decimal d) cell.Value = d;
                else if (value is bool b) cell.Value = b ? "Yes" : "No";
                else cell.Value = value?.ToString() ?? string.Empty;
            }
            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }
}
