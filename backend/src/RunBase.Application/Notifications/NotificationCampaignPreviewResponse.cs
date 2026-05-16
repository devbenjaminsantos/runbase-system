using RunBase.Domain.Notifications;
using RunBase.Domain.Plans;

namespace RunBase.Application.Notifications;

public sealed record NotificationCampaignPreviewResponse(
    NotificationCampaignType Type,
    PlanStage? TargetPlanStage,
    int EstimatedAudienceCount);
