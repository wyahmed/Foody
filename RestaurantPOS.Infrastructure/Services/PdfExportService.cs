using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Interfaces;

namespace RestaurantPOS.Infrastructure.Services;

/// <summary>PDF export service — generates thermal receipts and A4 invoices.</summary>
public class PdfExportService : IPdfExportService
{
    /// <inheritdoc/>
    public Task<byte[]> ExportInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        // TODO: Integrate a headless PDF library (e.g. PuppeteerSharp, WkHtmlToPdf, or QuestPDF)
        var placeholder = System.Text.Encoding.UTF8.GetBytes($"%PDF-1.4 placeholder invoice: {invoice.InvoiceNumber}");
        return Task.FromResult(placeholder);
    }

    /// <inheritdoc/>
    public Task<byte[]> ExportReportAsync(string templateName, object data, CancellationToken cancellationToken = default)
    {
        // TODO: Implement HTML template rendering + PDF conversion
        var placeholder = System.Text.Encoding.UTF8.GetBytes($"%PDF-1.4 placeholder report: {templateName}");
        return Task.FromResult(placeholder);
    }
}
