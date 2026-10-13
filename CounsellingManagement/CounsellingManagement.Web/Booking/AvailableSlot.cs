namespace CounsellingManagement.Web.Booking;

/// <summary>Represents an appointment start instant and its duration.</summary>
public sealed record AvailableSlot
{
    /// <summary>Creates a slot and stores its start time in UTC.</summary>
    public AvailableSlot(DateTimeOffset startsAt, int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        }

        StartsAtUtc = startsAt.ToUniversalTime();
        DurationMinutes = durationMinutes;
    }

    public DateTimeOffset StartsAtUtc { get; }

    public int DurationMinutes { get; }

    public DateTimeOffset EndsAtUtc => StartsAtUtc.AddMinutes(DurationMinutes);
}
