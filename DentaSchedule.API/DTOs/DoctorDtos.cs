namespace DentaSchedule.API.DTOs;

public class DoctorDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public Guid ClinicId { get; set; }
    public string? ClinicName { get; set; }
}

public class CreateDoctorDto
{
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public Guid ClinicId { get; set; }
}

public class UpdateDoctorDto
{
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public Guid ClinicId { get; set; }
}

// Schedule DTOs
public class DoctorScheduleDto
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotDurationMinutes { get; set; }
}

public class CreateScheduleDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotDurationMinutes { get; set; } = 30;
}

public class UpdateScheduleDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotDurationMinutes { get; set; }
}

// Exception DTOs
public class ScheduleExceptionDto
{
    public Guid Id { get; set; }
    public DateTime ExceptionDate { get; set; }
    public string? Reason { get; set; }
    public bool IsFullDayOff { get; set; }
    public string? CustomStart { get; set; }
    public string? CustomEnd { get; set; }
}

public class CreateExceptionDto
{
    public DateTime ExceptionDate { get; set; }
    public string? Reason { get; set; }
    public bool IsFullDayOff { get; set; } = true;
    public string? CustomStart { get; set; }
    public string? CustomEnd { get; set; }
}
