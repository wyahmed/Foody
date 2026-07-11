using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Application.Common.Exceptions;
using RestaurantPOS.Shared.Models;

namespace RestaurantPOS.Application.Features.Products.Queries;

// ============================================================
// DTOs
// ============================================================

public record ProductListDto(
    Guid Id,
    string Name,
    string NameAr,
    string? CategoryName,
    string? Barcode,
    string? SKU,
    decimal SellingPrice,
    decimal CostPrice,
    bool IsActive,
    int SortOrder,
    string? ImageUrl);

public record ProductDetailDto(
    Guid Id,
    Guid? CategoryId,
    string? CategoryName,
    Guid? BrandId,
    string? BrandName,
    Guid? UnitId,
    string? UnitName,
    Guid? TaxRateId,
    decimal? TaxRate,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    string? SKU,
    string? Barcode,
    Domain.Enums.ProductType ProductType,
    decimal CostPrice,
    decimal SellingPrice,
    bool TrackInventory,
    bool IsWeightBased,
    bool HasExpiry,
    decimal? MinStockLevel,
    decimal? MaxStockLevel,
    bool IsActive,
    int SortOrder,
    string? ImageUrl);

// ============================================================
// Queries
// ============================================================

/// <summary>Returns paged list of products.</summary>
public record GetProductsQuery(
    Guid TenantId,
    int PageNumber = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    bool? IsActive = null,
    string? Search = null
) : IRequest<PagedResult<ProductListDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductListDto>>
{
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;

    public GetProductsQueryHandler(Domain.Interfaces.IUnitOfWork unitOfWork)
        => _unitOfWork = unitOfWork;

    public async Task<PagedResult<ProductListDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Domain.Entities.Product>()
            .Query()
            .Include(p => p.Category)
            .Where(p => p.TenantId == request.TenantId);

        if (request.CategoryId.HasValue) query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        if (request.IsActive.HasValue) query = query.Where(p => p.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p => p.Name.Contains(request.Search)
                || p.NameAr.Contains(request.Search)
                || (p.Barcode != null && p.Barcode.Contains(request.Search))
                || (p.SKU != null && p.SKU.Contains(request.Search)));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductListDto(
                p.Id, p.Name, p.NameAr,
                p.Category != null ? p.Category.Name : null,
                p.Barcode, p.SKU, p.SellingPrice, p.CostPrice,
                p.IsActive, p.SortOrder, p.ImageUrl))
            .ToListAsync(cancellationToken);

        return PagedResult<ProductListDto>.Create(items, total, request.PageNumber, request.PageSize);
    }
}

/// <summary>Returns full product detail by ID.</summary>
public record GetProductByIdQuery(Guid Id) : IRequest<ProductDetailDto>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDetailDto>
{
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;

    public GetProductByIdQueryHandler(Domain.Interfaces.IUnitOfWork unitOfWork)
        => _unitOfWork = unitOfWork;

    public async Task<ProductDetailDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Repository<Domain.Entities.Product>()
            .Query()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.TaxRate)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null) throw new NotFoundException(nameof(Domain.Entities.Product), request.Id);

        return new ProductDetailDto(
            product.Id, product.CategoryId, product.Category?.Name,
            product.BrandId, product.Brand?.Name,
            product.UnitId, product.Unit?.Name,
            product.TaxRateId, product.TaxRate?.Rate,
            product.Name, product.NameAr, product.Description, product.DescriptionAr,
            product.SKU, product.Barcode, product.ProductType,
            product.CostPrice, product.SellingPrice,
            product.TrackInventory, product.IsWeightBased, product.HasExpiry,
            product.MinStockLevel, product.MaxStockLevel,
            product.IsActive, product.SortOrder, product.ImageUrl);
    }
}

/// <summary>Looks up a product by barcode.</summary>
public record GetProductByBarcodeQuery(string Barcode, Guid TenantId) : IRequest<ProductDetailDto?>;

public class GetProductByBarcodeQueryHandler : IRequestHandler<GetProductByBarcodeQuery, ProductDetailDto?>
{
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;

    public GetProductByBarcodeQueryHandler(Domain.Interfaces.IUnitOfWork unitOfWork)
        => _unitOfWork = unitOfWork;

    public async Task<ProductDetailDto?> Handle(GetProductByBarcodeQuery request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Repository<Domain.Entities.Product>()
            .Query()
            .Include(p => p.Category)
            .Include(p => p.TaxRate)
            .FirstOrDefaultAsync(p => p.Barcode == request.Barcode && p.TenantId == request.TenantId, cancellationToken);

        if (product is null) return null;

        return new ProductDetailDto(
            product.Id, product.CategoryId, product.Category?.Name,
            product.BrandId, null, product.UnitId, null,
            product.TaxRateId, product.TaxRate?.Rate,
            product.Name, product.NameAr, product.Description, product.DescriptionAr,
            product.SKU, product.Barcode, product.ProductType,
            product.CostPrice, product.SellingPrice,
            product.TrackInventory, product.IsWeightBased, product.HasExpiry,
            product.MinStockLevel, product.MaxStockLevel,
            product.IsActive, product.SortOrder, product.ImageUrl);
    }
}
