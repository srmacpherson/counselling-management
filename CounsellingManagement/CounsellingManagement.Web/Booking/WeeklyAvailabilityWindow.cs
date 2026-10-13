namespace CounsellingManagement.Web.Booking;

/// <summary>Associates a local availability window with a weekday.</summary>
public sealed record WeeklyAvailabilityWindow(DayOfWeek Day, LocalTimeWindow Window);
