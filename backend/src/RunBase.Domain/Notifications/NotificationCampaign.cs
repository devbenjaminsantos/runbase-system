using RunBase.Domain.Plans;

namespace RunBase.Domain.Notifications;

public sealed class NotificationCampaign
{
    public NotificationCampaign(
        Guid id,
        string name,
        NotificationCampaignType type,
        NotificationCampaignStatus status,
        PlanStage? targetPlanStage,
        DateTimeOffset? scheduledAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        Name = name;
        Type = type;
        Status = status;
        TargetPlanStage = targetPlanStage;
        ScheduledAt = scheduledAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public NotificationCampaignType Type { get; private set; }

    public NotificationCampaignStatus Status { get; private set; }

    public PlanStage? TargetPlanStage { get; private set; }

    public DateTimeOffset? ScheduledAt { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }
}
