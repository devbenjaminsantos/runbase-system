using Microsoft.EntityFrameworkCore;
using RunBase.Application.Orders;
using RunBase.Domain.Orders;
using RunBase.Infrastructure.Persistence;

namespace RunBase.Infrastructure.Orders;

public sealed class EfOrderRepository : IOrderRepository
{
    private readonly RunBaseDbContext _dbContext;

    public EfOrderRepository(RunBaseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Order>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public async Task SaveAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        var isTracked = _dbContext.ChangeTracker
            .Entries<Order>()
            .Any(entry => entry.Entity.Id == order.Id);

        if (!isTracked)
        {
            var exists = await _dbContext.Orders.AnyAsync(
                existingOrder => existingOrder.Id == order.Id,
                cancellationToken);

            if (exists)
            {
                _dbContext.Orders.Update(order);
            }
            else
            {
                await _dbContext.Orders.AddAsync(order, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Orders.Remove(order);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
