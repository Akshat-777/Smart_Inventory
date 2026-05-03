using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Abstractions.Repositories;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Inventory;
using InventoryManagement.Application.DTOs.Products;
using InventoryManagement.Application.Exceptions;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _db;
    private readonly AuditWriter _audit;
    private readonly ICurrentUserService _currentUser;

    public ProductService(
        IProductRepository products,
        IUnitOfWork unitOfWork,
        ApplicationDbContext db,
        AuditWriter audit,
        ICurrentUserService currentUser)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _products.GetByIdAsync(id, cancellationToken);
        return p == null ? null : Map(p);
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _products.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ProductDto>
        {
            Items = items.Select(Map).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        if (await _products.SkuExistsAsync(dto.Sku, null, cancellationToken))
            throw new BusinessRuleException($"SKU '{dto.Sku}' already exists.");

        var entity = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Sku = dto.Sku.Trim(),
            Category = dto.Category.Trim(),
            Price = dto.Price,
            LowStockThreshold = dto.LowStockThreshold
        };
        await _products.AddAsync(entity, cancellationToken);
        _audit.Enqueue(nameof(Product), entity.Id.ToString(), "Created", dto.Name, _currentUser.UserId, _currentUser.Email);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _products.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Product not found.");

        if (!string.Equals(entity.Sku, dto.Sku.Trim(), StringComparison.Ordinal) &&
            await _products.SkuExistsAsync(dto.Sku.Trim(), id, cancellationToken))
            throw new BusinessRuleException($"SKU '{dto.Sku}' already exists.");

        entity.Name = dto.Name.Trim();
        entity.Sku = dto.Sku.Trim();
        entity.Category = dto.Category.Trim();
        entity.Price = dto.Price;
        entity.LowStockThreshold = dto.LowStockThreshold;
        if (dto.RowVersion != null)
            _db.Entry(entity).Property(p => p.RowVersion).OriginalValue = dto.RowVersion;

        _audit.Enqueue(nameof(Product), entity.Id.ToString(), "Updated", entity.Name, _currentUser.UserId, _currentUser.Email);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }

        await _db.Entry(entity).ReloadAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _products.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Product not found.");

        var hasStock = await _db.InventoryItems.AsNoTracking()
            .AnyAsync(i => i.ProductId == id && i.Quantity > 0, cancellationToken);
        if (hasStock)
            throw new BusinessRuleException("Cannot delete a product that still has stock. Adjust inventory first.");

        _products.Remove(entity);
        _audit.Enqueue(nameof(Product), id.ToString(), "Deleted", entity.Name, _currentUser.UserId, _currentUser.Email);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
    }

    public async Task<IReadOnlyList<LowStockItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.Products.AsNoTracking().ToListAsync(cancellationToken);
        var totals = await _db.InventoryItems.AsNoTracking()
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.TotalQty, cancellationToken);

        var result = new List<LowStockItemDto>();
        foreach (var p in products)
        {
            var qty = totals.TryGetValue(p.Id, out var t) ? t : 0;
            if (qty <= p.LowStockThreshold)
            {
                result.Add(new LowStockItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Sku = p.Sku,
                    TotalQuantityAcrossWarehouses = qty,
                    LowStockThreshold = p.LowStockThreshold
                });
            }
        }

        return result.OrderBy(x => x.ProductName).ToList();
    }

    private static ProductDto Map(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Sku = p.Sku,
        Category = p.Category,
        Price = p.Price,
        LowStockThreshold = p.LowStockThreshold,
        RowVersion = p.RowVersion
    };
}
