using FluentValidation;
using MediatR;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Shared.Common;

namespace RestaurantPOS.Application.Features.Products.Commands;

// ============================================================
// Create Product
// ============================================================

/// <summary>Creates a new product in the catalog.</summary>
public record CreateProductCommand(
    Guid? CategoryId,
    Guid? BrandId,
    Guid? UnitId,
    Guid? TaxRateId,
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
    int SortOrder
) : IRequest<Result<Guid>>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate barcode
        if (!string.IsNullOrEmpty(request.Barcode))
        {
            var exists = await _unitOfWork.Repository<Product>()
                .ExistsAsync(p => p.Barcode == request.Barcode, cancellationToken);
            if (exists) return Result<Guid>.Failure("A product with this barcode already exists.", "DUPLICATE_BARCODE");
        }

        var product = new Product
        {
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            UnitId = request.UnitId,
            TaxRateId = request.TaxRateId,
            Name = request.Name,
            NameAr = request.NameAr,
            Description = request.Description,
            DescriptionAr = request.DescriptionAr,
            SKU = request.SKU,
            Barcode = request.Barcode,
            ProductType = request.ProductType,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            TrackInventory = request.TrackInventory,
            IsWeightBased = request.IsWeightBased,
            HasExpiry = request.HasExpiry,
            MinStockLevel = request.MinStockLevel,
            MaxStockLevel = request.MaxStockLevel,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        await _unitOfWork.Repository<Product>().AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(product.Id);
    }
}

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0).WithMessage("Cost price cannot be negative.");
        RuleFor(x => x.SellingPrice).GreaterThan(0).WithMessage("Selling price must be greater than 0.");
        RuleFor(x => x.SKU).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.SKU));
        RuleFor(x => x.Barcode).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Barcode));
    }
}

// ============================================================
// Update Product
// ============================================================

/// <summary>Updates an existing product.</summary>
public record UpdateProductCommand(
    Guid Id,
    Guid? CategoryId,
    Guid? BrandId,
    Guid? UnitId,
    Guid? TaxRateId,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    string? SKU,
    string? Barcode,
    decimal CostPrice,
    decimal SellingPrice,
    bool TrackInventory,
    decimal? MinStockLevel,
    decimal? MaxStockLevel,
    bool IsActive,
    int SortOrder
) : IRequest<Result>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Product>();
        var product = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (product is null) return Result.Failure("Product not found.");

        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.UnitId = request.UnitId;
        product.TaxRateId = request.TaxRateId;
        product.Name = request.Name;
        product.NameAr = request.NameAr;
        product.Description = request.Description;
        product.DescriptionAr = request.DescriptionAr;
        product.SKU = request.SKU;
        product.Barcode = request.Barcode;
        product.CostPrice = request.CostPrice;
        product.SellingPrice = request.SellingPrice;
        product.TrackInventory = request.TrackInventory;
        product.MinStockLevel = request.MinStockLevel;
        product.MaxStockLevel = request.MaxStockLevel;
        product.IsActive = request.IsActive;
        product.SortOrder = request.SortOrder;

        repo.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ============================================================
// Delete Product
// ============================================================

/// <summary>Soft-deletes a product.</summary>
public record DeleteProductCommand(Guid Id) : IRequest<Result>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Product>();
        var product = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (product is null) return Result.Failure("Product not found.");

        repo.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
