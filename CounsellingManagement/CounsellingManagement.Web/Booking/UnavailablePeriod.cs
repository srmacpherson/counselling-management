namespace CounsellingManagement.Web.Booking;

/// <summary>Represents a UTC interval when appointments cannot be scheduled.</summary>
public sealed record UnavailablePeriod
{
    /// <summary>Creates a block-out and normalizes both endpoints to UTC.</summary>
    public UnavailablePeriod(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("The end must be later than the start.", nameof(endsAt));
        }

        StartsAtUtc = startsAt.ToUniversalTime();
        EndsAtUtc = endsAt.ToUniversalTime();
    }

    public DateTimeOffset StartsAtUtc { get; }

    public DateTimeOffset EndsAtUtc { get; }
}
