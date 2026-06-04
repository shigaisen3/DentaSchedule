namespace DentaSchedule.DAL.Entities;

public enum AppointmentStatus
{
    Pending = 0,
    Approved = 1,
    Cancelled = 2
}

public class Appointment
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public DateTime AppointmentDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedByUserId { get; set; }

    // Concurrency
    public byte[] RowVersion { get; set; } = null!;

    // Foreign Keys
    public Guid DoctorId { get; set; }
    public Guid ClinicId { get; set; }

    // Navigation
    public Doctor Doctor { get; set; } = null!;
    public Clinic Clinic { get; set; } = null!;
}
