namespace TransManagement.Infrastructure.Chat;

public sealed class AiAssistantOptions
{
    public const string SectionName = "AiAssistant";

    public bool Enabled { get; set; } = true;

    public string Provider { get; set; } = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-2.0-flash";

    public string BotName { get; set; } = "Trợ lý AI TransFlow";
}

