namespace TransManagement.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendRegistrationOtpAsync(string recipientEmail, string otp, CancellationToken cancellationToken = default);
}
