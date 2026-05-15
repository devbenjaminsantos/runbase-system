namespace RunBase.Application.Security;

public sealed record SensitiveDataAccessAttempt(
    Guid? UserId,
    string? UserEmail,
    string ResourceType,
    Guid ResourceId,
    string SensitiveField);
