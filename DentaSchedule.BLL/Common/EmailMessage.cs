namespace DentaSchedule.BLL.Common;

/// <summary>A transport-agnostic email ready to be sent by an <c>IEmailService</c>.</summary>
public record EmailMessage(
    string ToEmail,
    string ToName,
    string Subject,
    string HtmlBody,
    string PlainBody);
