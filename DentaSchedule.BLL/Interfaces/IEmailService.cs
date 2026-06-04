using DentaSchedule.BLL.Common;

namespace DentaSchedule.BLL.Interfaces;

/// <summary>
/// Transport abstraction for sending an email. Implementations must never throw —
/// a notification failure should not break the business operation that triggered it.
/// </summary>
public interface IEmailService
{
    Task<bool> SendAsync(EmailMessage message);
}
