namespace DentaSchedule.DAL.Entities;

public class DoctorSchedule
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;

    // Foreign Keys
    public Guid DoctorId { get; set; }

    // Navigation
    public Doctor Doctor { get; set; } = null!;
}
