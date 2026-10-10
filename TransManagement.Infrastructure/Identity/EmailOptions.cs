namespace TransManagement.Infrastructure.Identity;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string SenderName { get; init; } = "TransFlow";
    public string SenderEmail { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string AppPassword { get; init; } = string.Empty;
}
