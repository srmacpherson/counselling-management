namespace CounsellingManagement.Web.Booking;

/// <summary>Represents a same-day local-time interval when sessions may start and finish.</summary>
public sealed record LocalTimeWindow
{
    /// <summary>Creates a window whose end is later than its start.</summary>
    public LocalTimeWindow(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
        {
            throw new ArgumentException("The end time must be later than the start time.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public TimeOnly Start { get; }

    public TimeOnly End { get; }
}
