using CounsellingManagement.Web.Booking;
using Xunit;

namespace CounsellingManagement.Tests;

/// <summary>Tests slot generation for weekly hours, overrides, and clock changes.</summary>
public class AvailabilityServiceTests
{
    private readonly BookingPolicy _policy = new();

    /// <summary>Creates a slot generator using the test booking policy.</summary>
    private AvailabilityService CreateService() => new(_policy);

    /// <summary>Returns quarter-hour starts only when the full session fits.</summary>
    [Fact]
    public void GetScheduledSlots_OffersQuarterHourStartsThatFitTheSession()
    {
        DateOnly monday = new(2026, 1, 5);
        AvailabilitySchedule schedule = CreateSchedule(
            new WeeklyAvailabilityWindow(DayOfWeek.Monday, Window(9, 0, 10, 0)));

        IReadOnlyList<AvailableSlot> slots = CreateService().GetScheduledSlots(schedule, monday, 30);

        Assert.Equal(
            [
                Utc(2026, 1, 5, 9, 0),
                Utc(2026, 1, 5, 9, 15),
                Utc(2026, 1, 5, 9, 30)
            ],
            slots.Select(slot => slot.StartsAtUtc));
    }

    /// <summary>Checks that a dated override replaces the weekly schedule.</summary>
    [Fact]
    public void GetScheduledSlots_DateOverrideReplacesWeeklyAvailability()
    {
        DateOnly monday = new(2026, 1, 5);
        AvailabilitySchedule schedule = new(
            [new WeeklyAvailabilityWindow(DayOfWeek.Monday, Window(9, 0, 12, 0))],
            [new DateAvailabilityOverride(monday, [Window(13, 0, 14, 0)])],
            []);

        IReadOnlyList<AvailableSlot> slots = CreateService().GetScheduledSlots(schedule, monday, 30);

        Assert.Equal(Utc(2026, 1, 5, 13, 0), slots[0].StartsAtUtc);
        Assert.DoesNotContain(slots, slot => slot.StartsAtUtc == Utc(2026, 1, 5, 9, 0));
    }

    /// <summary>Ensures nonexistent spring clock times are not offered.</summary>
    [Fact]
    public void GetScheduledSlots_SkipsNonexistentSpringClockTimes()
    {
        DateOnly springClockChange = new(2026, 3, 29);
        AvailabilitySchedule schedule = CreateSchedule(
            new WeeklyAvailabilityWindow(DayOfWeek.Sunday, Window(0, 0, 4, 0)));

        IReadOnlyList<AvailableSlot> slots = CreateService().GetScheduledSlots(schedule, springClockChange, 15);
        IEnumerable<DateTime> localTimes = slots.Select(slot => TimeZoneInfo.ConvertTime(slot.StartsAtUtc, _policy.BusinessTimeZone).DateTime);

        Assert.DoesNotContain(localTimes, local => local.Hour == 1);
        Assert.Contains(localTimes, local => local.Hour == 2);
    }

    /// <summary>Ensures both real instants of an autumn repeated time are offered.</summary>
    [Fact]
    public void GetScheduledSlots_OffersBothOccurrencesOfAnAmbiguousAutumnTime()
    {
        DateOnly autumnClockChange = new(2026, 10, 25);
        AvailabilitySchedule schedule = CreateSchedule(
            new WeeklyAvailabilityWindow(DayOfWeek.Sunday, Window(0, 0, 3, 0)));

        IReadOnlyList<AvailableSlot> slots = CreateService().GetScheduledSlots(schedule, autumnClockChange, 15);
        DateTimeOffset[] repeatedStarts = slots
            .Select(slot => TimeZoneInfo.ConvertTime(slot.StartsAtUtc, _policy.BusinessTimeZone))
            .Where(local => local.Hour == 1 && local.Minute == 15)
            .ToArray();

        Assert.Equal(2, repeatedStarts.Length);
        Assert.NotEqual(repeatedStarts[0].Offset, repeatedStarts[1].Offset);
    }

    /// <summary>Rejects sessions that leave a window during the autumn clock change.</summary>
    [Fact]
    public void GetScheduledSlots_RequiresTheWholeSessionToRemainWithinTheWindowAcrossClockChange()
    {
        DateOnly autumnClockChange = new(2026, 10, 25);
        AvailabilitySchedule schedule = CreateSchedule(
            new WeeklyAvailabilityWindow(DayOfWeek.Sunday, Window(1, 15, 1, 45)));

        IReadOnlyList<AvailableSlot> slots = CreateService().GetScheduledSlots(schedule, autumnClockChange, 30);
        IEnumerable<DateTimeOffset> localStarts = slots.Select(slot => TimeZoneInfo.ConvertTime(slot.StartsAtUtc, _policy.BusinessTimeZone));

        Assert.Equal(2, localStarts.Count(local => local.Hour == 1 && local.Minute == 15));
        Assert.DoesNotContain(localStarts, local => local.Hour == 1 && local.Minute == 30);
    }

    /// <summary>Creates a schedule containing only the supplied weekly windows.</summary>
    private static AvailabilitySchedule CreateSchedule(params WeeklyAvailabilityWindow[] weeklyWindows) =>
        new(weeklyWindows, [], []);

    /// <summary>Creates a same-day local-time window for a test.</summary>
    private static LocalTimeWindow Window(int startHour, int startMinute, int endHour, int endMinute) =>
        new(new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));

    /// <summary>Creates a UTC instant for a test case.</summary>
    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
