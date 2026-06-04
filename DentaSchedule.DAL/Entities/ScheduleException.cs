namespace DentaSchedule.DAL.Entities;

public class ScheduleException
{
    public Guid Id { get; set; }
    public DateTime ExceptionDate { get; set; }
    public string? Reason { get; set; }
    public bool IsFullDayOff { get; set; } = true;
    public TimeSpan? CustomStart { get; set; }
    public TimeSpan? CustomEnd { get; set; }

    // Foreign Keys
    public Guid DoctorId { get; set; }

    // Navigation
    public Doctor Doctor { get; set; } = null!;
}
