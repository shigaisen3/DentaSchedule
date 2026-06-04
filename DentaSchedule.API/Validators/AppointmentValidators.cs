using DentaSchedule.API.DTOs;
using FluentValidation;

namespace DentaSchedule.API.Validators;

public class CreateAppointmentDtoValidator : AbstractValidator<CreateAppointmentDto>
{
    public CreateAppointmentDtoValidator()
    {
        RuleFor(x => x.PatientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PatientEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.PatientPhone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.AppointmentDateTime).GreaterThan(DateTime.UtcNow)
            .WithMessage("Appointment must be in the future.");
        RuleFor(x => x.DurationMinutes).GreaterThan(0).LessThanOrEqualTo(240);
    }
}

public class RescheduleDtoValidator : AbstractValidator<RescheduleDto>
{
    public RescheduleDtoValidator()
    {
        RuleFor(x => x.NewDateTime).GreaterThan(DateTime.UtcNow)
            .WithMessage("New date/time must be in the future.");
    }
}

public class CancelDtoValidator : AbstractValidator<CancelDto>
{
    public CancelDtoValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
