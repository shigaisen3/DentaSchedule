namespace DentaSchedule.BLL.Common;

/// <summary>
/// Bound from the <c>Email</c> section of configuration. When <see cref="UseSmtp"/> is
/// false (the default), the app uses a logging email service that requires no credentials,
/// so the project runs out of the box.
/// </summary>
public class EmailSettings
{
    public bool UseSmtp { get; set; }
    public string FromName { get; set; } = "DentaSchedule";
    public string FromAddress { get; set; } = "no-reply@dentaschedule.local";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
}
