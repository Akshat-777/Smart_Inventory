using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Abstractions.Repositories;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Warehouses;
using InventoryManagement.Application.Exceptions;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _db;
    private readonly AuditWriter _audit;
    private readonly ICurrentUserService _currentUser;

    public WarehouseService(
        IWarehouseRepository warehouses,
        IUnitOfWork unitOfWork,
        ApplicationDbContext db,
        AuditWriter audit,
        ICurrentUserService currentUser)
    {
        _warehouses = warehouses;
        _unitOfWork = unitOfWork;
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var w = await _warehouses.GetByIdAsync(id, cancellationToken);
        return w == null ? null : Map(w);
    }

    public async Task<PagedResult<WarehouseDto>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _warehouses.GetPagedAsync(query, cancellationToken);
        return new PagedResult<WarehouseDto>
        {
            Items = items.Select(Map).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Location = dto.Location.Trim()
        };
        await _warehouses.AddAsync(entity, cancellationToken);
        _audit.Enqueue(nameof(Warehouse), entity.Id.ToString(), "Created", entity.Name, _currentUser.UserId, _currentUser.Email);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _warehouses.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Warehouse not found.");

        entity.Name = dto.Name.Trim();
        entity.Location = dto.Location.Trim();
        if (dto.RowVersion != null)
            _db.Entry(entity).Property(w => w.RowVersion).OriginalValue = dto.RowVersion;

        _audit.Enqueue(nameof(Warehouse), entity.Id.ToString(), "Updated", entity.Name, _currentUser.UserId, _currentUser.Email);

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
        var entity = await _warehouses.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Warehouse not found.");

        var hasStock = await _db.InventoryItems.AsNoTracking()
            .AnyAsync(i => i.WarehouseId == id && i.Quantity > 0, cancellationToken);
        if (hasStock)
            throw new BusinessRuleException("Cannot delete a warehouse that still holds stock.");

        _warehouses.Remove(entity);
        _audit.Enqueue(nameof(Warehouse), id.ToString(), "Deleted", entity.Name, _currentUser.UserId, _currentUser.Email);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
    }

    private static WarehouseDto Map(Warehouse w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        Location = w.Location,
        RowVersion = w.RowVersion
    };
}
