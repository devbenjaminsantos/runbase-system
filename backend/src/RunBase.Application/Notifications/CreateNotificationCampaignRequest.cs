using System.ComponentModel.DataAnnotations;
using RunBase.Domain.Notifications;
using RunBase.Domain.Plans;

namespace RunBase.Application.Notifications;

public sealed record CreateNotificationCampaignRequest(
    [property: Required]
    [property: StringLength(120, MinimumLength = 3)]
    string Name,
    NotificationCampaignType Type,
    PlanStage? TargetPlanStage = null,
    DateTimeOffset? ScheduledAt = null);
