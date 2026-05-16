namespace RunBase.Application.Notifications;

public sealed class NotificationCampaignResult<T>
{
    private NotificationCampaignResult(T? value, NotificationCampaignError error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public NotificationCampaignError Error { get; }

    public bool Succeeded => Error == NotificationCampaignError.None;

    public static NotificationCampaignResult<T> Success(T value)
    {
        return new NotificationCampaignResult<T>(value, NotificationCampaignError.None);
    }

    public static NotificationCampaignResult<T> Failure(NotificationCampaignError error)
    {
        return new NotificationCampaignResult<T>(default, error);
    }
}
