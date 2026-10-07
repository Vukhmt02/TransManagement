namespace TransManagement.Application.Common.Identity;

public sealed record UserProfile(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles);

