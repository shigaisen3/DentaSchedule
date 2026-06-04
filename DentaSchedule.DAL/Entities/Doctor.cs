namespace DentaSchedule.DAL.Entities;

public class Doctor
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Foreign Keys
    public Guid ClinicId { get; set; }

    // Navigation
    public Clinic Clinic { get; set; } = null!;
    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
    public ICollection<ScheduleException> Exceptions { get; set; } = new List<ScheduleException>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
