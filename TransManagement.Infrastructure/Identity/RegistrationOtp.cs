namespace TransManagement.Infrastructure.Identity;

public sealed class RegistrationOtp
{
    private RegistrationOtp() { }

    public RegistrationOtp(string normalizedEmail, string codeHash, string salt, DateTime nowUtc)
    {
        Id = Guid.NewGuid();
        NormalizedEmail = normalizedEmail;
        Replace(codeHash, salt, nowUtc);
    }

    public Guid Id { get; private set; }
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string CodeHash { get; private set; } = string.Empty;
    public string Salt { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime LastSentAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }
    public bool IsUsed { get; private set; }

    public void Replace(string codeHash, string salt, DateTime nowUtc)
    {
        CodeHash = codeHash;
        Salt = salt;
        LastSentAtUtc = nowUtc;
        ExpiresAtUtc = nowUtc.AddMinutes(5);
        FailedAttempts = 0;
        IsUsed = false;
    }

    public void RecordFailure() => FailedAttempts++;
    public void MarkUsed() => IsUsed = true;
    public void AllowImmediateRetry() => LastSentAtUtc = DateTime.UnixEpoch;
}
