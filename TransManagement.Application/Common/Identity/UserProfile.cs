namespace TransManagement.Application.Common.Identity;

public sealed record UserProfile(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles);

public sealed record UserSummary(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

