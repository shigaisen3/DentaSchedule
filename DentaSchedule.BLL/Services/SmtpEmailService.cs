using DentaSchedule.BLL.Common;
using DentaSchedule.BLL.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace DentaSchedule.BLL.Services;

/// <summary>
/// Sends email over SMTP using MailKit. Enabled by setting <c>Email:UseSmtp</c> to true
/// and supplying host/port/credentials. Any failure is logged and swallowed so that a
/// down mail server never fails an appointment operation.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(EmailSettings settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> SendAsync(EmailMessage message)
    {
        try
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            mime.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
            mime.Subject = message.Subject;
            mime.Body = new BodyBuilder
            {
                HtmlBody = message.HtmlBody,
                TextBody = message.PlainBody
            }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTlsWhenAvailable);

            if (!string.IsNullOrEmpty(_settings.Username))
                await client.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty);

            await client.SendAsync(mime);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Email} (subject: {Subject})", message.ToEmail, message.Subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", message.ToEmail);
            return false;
        }
    }
}
