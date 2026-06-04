using System.Globalization;
using DentaSchedule.BLL.Common;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using DentaSchedule.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace DentaSchedule.BLL.Services;

/// <summary>
/// Builds the Romanian-language email for each appointment lifecycle event and hands it
/// to the configured <see cref="IEmailService"/>. Doctor and clinic names are resolved
/// from the entity navigations when available, otherwise looked up by id. Every public
/// method is exception-safe so notification problems never break a booking operation.
/// </summary>
public class AppointmentNotificationService : IAppointmentNotificationService
{
    private readonly IEmailService _email;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AppointmentNotificationService> _logger;

    private static readonly CultureInfo Ro = CultureInfo.GetCultureInfo("ro-RO");

    public AppointmentNotificationService(
        IEmailService email,
        IUnitOfWork unitOfWork,
        ILogger<AppointmentNotificationService> logger)
    {
        _email = email;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task NotifyCreatedAsync(Appointment appointment) =>
        SendAsync(appointment,
            subject: $"Programare înregistrată — {appointment.ReferenceNumber}",
            intro: "Cererea ta de programare a fost înregistrată și este în așteptarea confirmării.",
            footer: "Vei primi un email de confirmare după ce programarea este aprobată de clinică.");

    public Task NotifyApprovedAsync(Appointment appointment) =>
        SendAsync(appointment,
            subject: $"Programare confirmată — {appointment.ReferenceNumber}",
            intro: "Programarea ta a fost confirmată. Te așteptăm!",
            footer: "Dacă nu mai poți ajunge, te rugăm să anunți clinica din timp.");

    public Task NotifyCancelledAsync(Appointment appointment) =>
        SendAsync(appointment,
            subject: $"Programare anulată — {appointment.ReferenceNumber}",
            intro: "Programarea ta a fost anulată."
                   + (string.IsNullOrWhiteSpace(appointment.CancellationReason)
                       ? string.Empty
                       : $" Motiv: {appointment.CancellationReason}"),
            footer: "Poți face o nouă programare oricând de pe site-ul nostru.");

    public Task NotifyRescheduledAsync(Appointment appointment) =>
        SendAsync(appointment,
            subject: $"Programare reprogramată — {appointment.ReferenceNumber}",
            intro: "Programarea ta a fost mutată la o nouă dată și oră, prezentate mai jos.",
            footer: "Noua programare este în așteptarea confirmării.");

    private async Task SendAsync(Appointment appointment, string subject, string intro, string footer)
    {
        try
        {
            var (doctorName, clinicName) = await ResolveNamesAsync(appointment);
            var when = appointment.AppointmentDateTime.ToString("dddd, dd MMMM yyyy, HH:mm", Ro);

            var plain =
                $"Bună, {appointment.PatientName},\n\n" +
                $"{intro}\n\n" +
                $"Detalii programare:\n" +
                $"  • Cod: {appointment.ReferenceNumber}\n" +
                $"  • Medic: {doctorName}\n" +
                $"  • Clinică: {clinicName}\n" +
                $"  • Data: {when}\n" +
                $"  • Durată: {appointment.DurationMinutes} minute\n\n" +
                $"{footer}\n\n" +
                "Cu stimă,\nEchipa DentaSchedule";

            var html =
                $"<p>Bună, {appointment.PatientName},</p>" +
                $"<p>{intro}</p>" +
                "<table cellpadding=\"4\">" +
                $"<tr><td><strong>Cod</strong></td><td>{appointment.ReferenceNumber}</td></tr>" +
                $"<tr><td><strong>Medic</strong></td><td>{doctorName}</td></tr>" +
                $"<tr><td><strong>Clinică</strong></td><td>{clinicName}</td></tr>" +
                $"<tr><td><strong>Data</strong></td><td>{when}</td></tr>" +
                $"<tr><td><strong>Durată</strong></td><td>{appointment.DurationMinutes} minute</td></tr>" +
                "</table>" +
                $"<p>{footer}</p>" +
                "<p>Cu stimă,<br/>Echipa DentaSchedule</p>";

            await _email.SendAsync(new EmailMessage(
                appointment.PatientEmail, appointment.PatientName, subject, html, plain));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for appointment {Reference}",
                appointment.ReferenceNumber);
        }
    }

    private async Task<(string doctorName, string clinicName)> ResolveNamesAsync(Appointment appointment)
    {
        var doctorName = appointment.Doctor?.FullName
            ?? (await _unitOfWork.Doctors.GetByIdAsync(appointment.DoctorId))?.FullName
            ?? "—";

        var clinicName = appointment.Clinic?.Name
            ?? (await _unitOfWork.Clinics.GetByIdAsync(appointment.ClinicId))?.Name
            ?? "—";

        return (doctorName, clinicName);
    }
}
