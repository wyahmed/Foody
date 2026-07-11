namespace RestaurantPOS.Domain.Interfaces;

/// <summary>Provides the current user context (tenant, user id, etc.).</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    Guid? BranchId { get; }
    string? UserName { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
}

/// <summary>Provides the current date/time (allows testing with fixed times).</summary>
public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }
}

/// <summary>Cache abstraction over Redis or in-memory.</summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}

/// <summary>Generates sequential invoice, order, PO numbers.</summary>
public interface INumberGenerator
{
    Task<string> GenerateOrderNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> GenerateInvoiceNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> GeneratePurchaseOrderNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> GenerateShiftNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
}

/// <summary>Handles real-time notifications via SignalR.</summary>
public interface INotificationService
{
    Task SendToUserAsync(string userId, string eventName, object payload, CancellationToken cancellationToken = default);
    Task SendToBranchAsync(string branchId, string eventName, object payload, CancellationToken cancellationToken = default);
    Task SendToAllAsync(string eventName, object payload, CancellationToken cancellationToken = default);
}

/// <summary>Generates ZATCA-compliant QR codes and XML invoices.</summary>
public interface IZatcaService
{
    Task<string> GenerateQrCodeAsync(Entities.Invoice invoice, CancellationToken cancellationToken = default);
    Task<string> GenerateXmlAsync(Entities.Invoice invoice, CancellationToken cancellationToken = default);
    Task<string> SignXmlAsync(string xmlContent, CancellationToken cancellationToken = default);
    Task<string> ComputeHashAsync(string xmlContent, CancellationToken cancellationToken = default);
    Task<ZatcaReportResult> ReportInvoiceAsync(Entities.Invoice invoice, CancellationToken cancellationToken = default);
    Task<ZatcaReportResult> ClearInvoiceAsync(Entities.Invoice invoice, CancellationToken cancellationToken = default);
}

/// <summary>Result from a ZATCA API call.</summary>
public record ZatcaReportResult(bool IsSuccess, string? RequestId, string? Warnings, string? Errors);

/// <summary>Exports data to PDF.</summary>
public interface IPdfExportService
{
    Task<byte[]> ExportInvoiceAsync(Entities.Invoice invoice, CancellationToken cancellationToken = default);
    Task<byte[]> ExportReportAsync(string templateName, object data, CancellationToken cancellationToken = default);
}

/// <summary>Exports data to Excel.</summary>
public interface IExcelExportService
{
    Task<byte[]> ExportAsync<T>(IEnumerable<T> data, string sheetName = "Sheet1", CancellationToken cancellationToken = default);
}
