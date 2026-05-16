using RunBase.Domain.Notifications;
using RunBase.Domain.Plans;

namespace RunBase.Application.Notifications;

public sealed record NotificationCampaignResponse(
    Guid Id,
    string Name,
    NotificationCampaignType Type,
    NotificationCampaignStatus Status,
    PlanStage? TargetPlanStage,
    int EstimatedAudienceCount,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
