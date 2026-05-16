using Microsoft.EntityFrameworkCore;
using RunBase.Application.Plans;
using RunBase.Domain.Plans;
using RunBase.Infrastructure.Persistence;

namespace RunBase.Infrastructure.Plans;

public sealed class EfPlanRepository : IPlanRepository
{
    private readonly RunBaseDbContext _dbContext;

    public EfPlanRepository(RunBaseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Plan>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Plans
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Plan?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Plans
            .FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);
    }

    public async Task<bool> StageExistsAsync(
        PlanStage stage,
        Guid? exceptPlanId = null,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Plans.AnyAsync(
            plan => (!exceptPlanId.HasValue || plan.Id != exceptPlanId.Value) && plan.Stage == stage,
            cancellationToken);
    }

    public async Task SaveAsync(
        Plan plan,
        CancellationToken cancellationToken = default)
    {
        var isTracked = _dbContext.ChangeTracker
            .Entries<Plan>()
            .Any(entry => entry.Entity.Id == plan.Id);

        if (!isTracked)
        {
            var exists = await _dbContext.Plans.AnyAsync(
                existingPlan => existingPlan.Id == plan.Id,
                cancellationToken);

            if (exists)
            {
                _dbContext.Plans.Update(plan);
            }
            else
            {
                await _dbContext.Plans.AddAsync(plan, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Plan plan,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Plans.Remove(plan);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
