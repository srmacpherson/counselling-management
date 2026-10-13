using CounsellingManagement.Web.Booking;
using Xunit;

namespace CounsellingManagement.Tests;

/// <summary>Tests booking limits, conflicts, and validation outcomes.</summary>
public class BookingServiceTests
{
    private static readonly BookingPolicy Policy = new();
    private static readonly BookingService Service = new(new AvailabilityService(Policy), Policy);
    private static readonly DateOnly Monday = new(2026, 1, 5);
    private static readonly DateTimeOffset Now = Utc(2026, 1, 5, 8, 0);

    /// <summary>Allows future same-day starts and excludes starts already past.</summary>
    [Fact]
    public void GetAvailableSlots_AllowsFutureSameDayStartsButExcludesPastStarts()
    {
        AvailabilitySchedule schedule = MondaySchedule(Window(9, 0, 10, 0));
        DateTimeOffset afterFirstStart = Utc(2026, 1, 5, 9, 16);

        IReadOnlyList<AvailableSlot> slots = Service.GetAvailableSlots(schedule, Monday, 15, [], afterFirstStart);

        Assert.Equal([Utc(2026, 1, 5, 9, 30), Utc(2026, 1, 5, 9, 45)], slots.Select(slot => slot.StartsAtUtc));
    }

    /// <summary>Limits offered dates to the configured 90-day horizon.</summary>
    [Fact]
    public void GetAvailableSlots_EnforcesNinetyDayCalendarHorizon()
    {
        DateOnly withinHorizon = Monday.AddDays(90);
        DateOnly beyondHorizon = Monday.AddDays(91);
        AvailabilitySchedule schedule = new(
            [new WeeklyAvailabilityWindow(withinHorizon.DayOfWeek, Window(9, 0, 10, 0))],
            [],
            []);

        Assert.NotEmpty(Service.GetAvailableSlots(schedule, withinHorizon, 15, [], Now));
        Assert.Empty(Service.GetAvailableSlots(schedule, beyondHorizon, 15, [], Now));
    }

    /// <summary>Blocks overlapping confirmed sessions but permits back-to-back sessions.</summary>
    [Fact]
    public void GetAvailableSlots_ExcludesOverlappingConfirmedAppointmentsButAllowsBackToBackSessions()
    {
        AvailabilitySchedule schedule = MondaySchedule(Window(9, 0, 10, 0));
        Appointment appointment = new(Utc(2026, 1, 5, 9, 15), 30);

        IReadOnlyList<AvailableSlot> slots = Service.GetAvailableSlots(schedule, Monday, 15, [appointment], Now);

        Assert.Equal([Utc(2026, 1, 5, 9, 0), Utc(2026, 1, 5, 9, 45)], slots.Select(slot => slot.StartsAtUtc));
    }

    /// <summary>Frees the time reserved by a cancelled appointment.</summary>
    [Fact]
    public void GetAvailableSlots_CancelledAppointmentDoesNotReserveTime()
    {
        AvailabilitySchedule schedule = MondaySchedule(Window(9, 0, 10, 0));
        Appointment cancelledAppointment = new(Utc(2026, 1, 5, 9, 15), 30, AppointmentStatus.Cancelled);

        IReadOnlyList<AvailableSlot> slots = Service.GetAvailableSlots(schedule, Monday, 15, [cancelledAppointment], Now);

        Assert.Equal(4, slots.Count);
    }

    /// <summary>Excludes slots that overlap a counsellor block-out.</summary>
    [Fact]
    public void GetAvailableSlots_ExcludesPeriodsBlockedByTheCounsellor()
    {
        AvailabilitySchedule schedule = new(
            [new WeeklyAvailabilityWindow(DayOfWeek.Monday, Window(9, 0, 10, 0))],
            [],
            [new UnavailablePeriod(Utc(2026, 1, 5, 9, 15), Utc(2026, 1, 5, 9, 45))]);

        IReadOnlyList<AvailableSlot> slots = Service.GetAvailableSlots(schedule, Monday, 15, [], Now);

        Assert.Equal([Utc(2026, 1, 5, 9, 0), Utc(2026, 1, 5, 9, 45)], slots.Select(slot => slot.StartsAtUtc));
    }

    /// <summary>Reports when a requested session length is not offered.</summary>
    [Fact]
    public void ValidateBooking_RejectsUnofferedDuration()
    {
        BookingValidationResult result = Service.ValidateBooking(
            MondaySchedule(Window(9, 0, 10, 0)),
            Utc(2026, 1, 5, 9, 0),
            17,
            [],
            Now);

        Assert.Equal(BookingValidationError.SessionDurationNotOffered, result.Error);
    }

    /// <summary>Reports when the requested time conflicts with a confirmed appointment.</summary>
    [Fact]
    public void ValidateBooking_ReportsAppointmentConflict()
    {
        Appointment appointment = new(Utc(2026, 1, 5, 9, 0), 30);

        BookingValidationResult result = Service.ValidateBooking(
            MondaySchedule(Window(9, 0, 10, 0)),
            Utc(2026, 1, 5, 9, 15),
            15,
            [appointment],
            Now);

        Assert.Equal(BookingValidationError.AppointmentConflict, result.Error);
    }

    /// <summary>Creates a Monday schedule for a test case.</summary>
    private static AvailabilitySchedule MondaySchedule(LocalTimeWindow window) =>
        new([new WeeklyAvailabilityWindow(DayOfWeek.Monday, window)], [], []);

    /// <summary>Creates a same-day local-time window for a test.</summary>
    private static LocalTimeWindow Window(int startHour, int startMinute, int endHour, int endMinute) =>
        new(new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));

    /// <summary>Creates a UTC instant for a test case.</summary>
    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
