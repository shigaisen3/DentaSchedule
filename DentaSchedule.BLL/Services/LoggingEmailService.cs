using DentaSchedule.BLL.Common;
using DentaSchedule.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace DentaSchedule.BLL.Services;

/// <summary>
/// Default <see cref="IEmailService"/> implementation: it logs the email instead of
/// sending it. This keeps the system fully functional without SMTP credentials —
/// useful for development, demos, and tests. Swap in <see cref="SmtpEmailService"/>
/// (via the <c>Email:UseSmtp</c> setting) to deliver real mail.
/// </summary>
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(EmailMessage message)
    {
        _logger.LogInformation(
            "[EMAIL] To: {Name} <{Email}> | Subject: {Subject}\n{Body}",
            message.ToName, message.ToEmail, message.Subject, message.PlainBody);

        return Task.FromResult(true);
    }
}
