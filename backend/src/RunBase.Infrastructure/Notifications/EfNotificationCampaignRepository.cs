using Microsoft.EntityFrameworkCore;
using RunBase.Application.Notifications;
using RunBase.Domain.Notifications;
using RunBase.Infrastructure.Persistence;

namespace RunBase.Infrastructure.Notifications;

public sealed class EfNotificationCampaignRepository : INotificationCampaignRepository
{
    private readonly RunBaseDbContext _dbContext;

    public EfNotificationCampaignRepository(RunBaseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<NotificationCampaign>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationCampaigns
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationCampaign?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationCampaigns
            .FirstOrDefaultAsync(campaign => campaign.Id == id, cancellationToken);
    }

    public async Task SaveAsync(
        NotificationCampaign campaign,
        CancellationToken cancellationToken = default)
    {
        var isTracked = _dbContext.ChangeTracker
            .Entries<NotificationCampaign>()
            .Any(entry => entry.Entity.Id == campaign.Id);

        if (!isTracked)
        {
            var exists = await _dbContext.NotificationCampaigns.AnyAsync(
                existingCampaign => existingCampaign.Id == campaign.Id,
                cancellationToken);

            if (exists)
            {
                _dbContext.NotificationCampaigns.Update(campaign);
            }
            else
            {
                await _dbContext.NotificationCampaigns.AddAsync(campaign, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
