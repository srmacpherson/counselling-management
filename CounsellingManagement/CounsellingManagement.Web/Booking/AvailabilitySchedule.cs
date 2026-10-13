namespace CounsellingManagement.Web.Booking;

/// <summary>Groups recurring hours, dated overrides, and unavailable periods.</summary>
public sealed class AvailabilitySchedule
{
    /// <summary>Copies schedule rules and rejects duplicate overrides for a date.</summary>
    public AvailabilitySchedule(
        IEnumerable<WeeklyAvailabilityWindow> weeklyWindows,
        IEnumerable<DateAvailabilityOverride> dateOverrides,
        IEnumerable<UnavailablePeriod> unavailablePeriods)
    {
        ArgumentNullException.ThrowIfNull(weeklyWindows);
        ArgumentNullException.ThrowIfNull(dateOverrides);
        ArgumentNullException.ThrowIfNull(unavailablePeriods);

        WeeklyWindows = Array.AsReadOnly(weeklyWindows.ToArray());
        DateOverrides = Array.AsReadOnly(dateOverrides.ToArray());
        UnavailablePeriods = Array.AsReadOnly(unavailablePeriods.ToArray());

        if (DateOverrides.Select(item => item.Date).Distinct().Count() != DateOverrides.Count)
        {
            throw new ArgumentException("Only one availability override may be defined for each date.", nameof(dateOverrides));
        }
    }

    public IReadOnlyList<WeeklyAvailabilityWindow> WeeklyWindows { get; }

    public IReadOnlyList<DateAvailabilityOverride> DateOverrides { get; }

    public IReadOnlyList<UnavailablePeriod> UnavailablePeriods { get; }
}
