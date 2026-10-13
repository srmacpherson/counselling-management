namespace CounsellingManagement.Web.Booking;

/// <summary>Describes whether an appointment reserves its time.</summary>
public enum AppointmentStatus
{
    Confirmed,
    Cancelled
}

/// <summary>Represents an appointment with a UTC start, duration, and status.</summary>
public sealed record Appointment
{
    /// <summary>Creates an appointment and normalizes its start time to UTC.</summary>
    public Appointment(DateTimeOffset startsAt, int durationMinutes, AppointmentStatus status = AppointmentStatus.Confirmed)
    {
        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Appointment duration must be positive.");
        }

        StartsAtUtc = startsAt.ToUniversalTime();
        DurationMinutes = durationMinutes;
        Status = status;
    }

    public DateTimeOffset StartsAtUtc { get; }

    public int DurationMinutes { get; }

    public DateTimeOffset EndsAtUtc => StartsAtUtc.AddMinutes(DurationMinutes);

    public AppointmentStatus Status { get; }
}
