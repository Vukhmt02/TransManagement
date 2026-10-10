using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TransManagement.Application.Common.Interfaces;

namespace TransManagement.Infrastructure.Identity;

public sealed class GmailEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendRegistrationOtpAsync(string recipientEmail, string otp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.AppPassword))
            throw new InvalidOperationException("Gmail SMTP credentials are not configured.");

        using var message = new MailMessage
        {
            From = new MailAddress(string.IsNullOrWhiteSpace(_options.SenderEmail) ? _options.Username : _options.SenderEmail, _options.SenderName),
            Subject = $"{otp} là mã xác thực TransFlow của bạn",
            Body = $"""
                <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;padding:28px;color:#183044">
                  <h2 style="color:#108f85">Xác thực tài khoản TransFlow</h2>
                  <p>Mã OTP đăng ký của bạn là:</p>
                  <div style="font-size:34px;font-weight:800;letter-spacing:10px;padding:18px;background:#eef8f7;text-align:center;border-radius:10px">{otp}</div>
                  <p>Mã có hiệu lực trong <b>5 phút</b>. Không chia sẻ mã này với bất kỳ ai.</p>
                  <p style="color:#7b8994;font-size:12px">Nếu bạn không yêu cầu đăng ký, hãy bỏ qua email này.</p>
                </div>
                """,
            IsBodyHtml = true
        };
        message.To.Add(recipientEmail);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.Username, _options.AppPassword)
        };
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
