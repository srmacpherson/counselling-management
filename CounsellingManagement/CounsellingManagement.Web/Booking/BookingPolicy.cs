namespace CounsellingManagement.Web.Booking;

/// <summary>Defines the time zone, offered session lengths, and booking limits.</summary>
public sealed class BookingPolicy
{
    public const string LondonTimeZoneId = "Europe/London";
    public const int MinimumSessionDurationMinutes = 15;
    public const int MaximumSessionDurationMinutes = 90;
    public static IReadOnlyList<int> SupportedSessionDurationsMinutes { get; } =
        Array.AsReadOnly(new[] { 15, 30, 45, 60, 90 });

    /// <summary>Creates validated booking settings with London-friendly defaults.</summary>
    public BookingPolicy(
        TimeZoneInfo? businessTimeZone = null,
        IEnumerable<int>? offeredSessionDurationsMinutes = null,
        int startIntervalMinutes = 15,
        int bookingHorizonDays = 90,
        TimeSpan? minimumNotice = null,
        int bufferMinutes = 0)
    {
        BusinessTimeZone = businessTimeZone ?? TimeZoneInfo.FindSystemTimeZoneById(LondonTimeZoneId);

        IEnumerable<int> durations = offeredSessionDurationsMinutes ?? SupportedSessionDurationsMinutes;

        OfferedSessionDurationsMinutes = Array.AsReadOnly(durations.Distinct().Order().ToArray());
        if (OfferedSessionDurationsMinutes.Count == 0 || OfferedSessionDurationsMinutes.Any(duration => !IsValidSessionDuration(duration)))
        {
            throw new ArgumentException("Offered durations must be 15, 30, 45, 60, or 90 minutes.", nameof(offeredSessionDurationsMinutes));
        }

        if (startIntervalMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startIntervalMinutes));
        }

        if (bookingHorizonDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bookingHorizonDays));
        }

        if (minimumNotice < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumNotice));
        }

        if (bufferMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferMinutes));
        }

        StartIntervalMinutes = startIntervalMinutes;
        BookingHorizonDays = bookingHorizonDays;
        MinimumNotice = minimumNotice ?? TimeSpan.Zero;
        Buffer = TimeSpan.FromMinutes(bufferMinutes);
    }

    public TimeZoneInfo BusinessTimeZone { get; }

    public IReadOnlyList<int> OfferedSessionDurationsMinutes { get; }

    public int StartIntervalMinutes { get; }

    public int BookingHorizonDays { get; }

    public TimeSpan MinimumNotice { get; }

    public TimeSpan Buffer { get; }

    /// <summary>Checks whether a duration is one of the supported session lengths.</summary>
    public static bool IsValidSessionDuration(int durationMinutes) =>
        SupportedSessionDurationsMinutes.Contains(durationMinutes);
}
