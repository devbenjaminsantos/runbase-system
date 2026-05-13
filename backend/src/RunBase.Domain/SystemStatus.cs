namespace RunBase.Domain;

public sealed record SystemStatus(
    string Service,
    string Status,
    DateTimeOffset TimestampUtc);
