namespace CounsellingManagement.Web.Booking;

/// <summary>Replaces weekly availability with custom windows for one date.</summary>
public sealed class DateAvailabilityOverride
{
    /// <summary>Creates an override; an empty window list marks the date unavailable.</summary>
    public DateAvailabilityOverride(DateOnly date, IEnumerable<LocalTimeWindow> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        Date = date;
        Windows = Array.AsReadOnly(windows.ToArray());
    }

    public DateOnly Date { get; }

    public IReadOnlyList<LocalTimeWindow> Windows { get; }
}
