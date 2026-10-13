namespace CounsellingManagement.Web.Booking;

/// <summary>Builds appointment start times from local availability windows.</summary>
public sealed class AvailabilityService(BookingPolicy policy)
{
    /// <summary>Returns configured start times whose full duration fits a window.</summary>
    public IReadOnlyList<AvailableSlot> GetScheduledSlots(
        AvailabilitySchedule schedule,
        DateOnly date,
        int durationMinutes)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (!policy.OfferedSessionDurationsMinutes.Contains(durationMinutes))
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "The selected session duration is not offered.");
        }

        DateAvailabilityOverride? dateOverride = schedule.DateOverrides.SingleOrDefault(item => item.Date == date);
        IReadOnlyList<LocalTimeWindow> windows = dateOverride?.Windows ?? schedule.WeeklyWindows
            .Where(item => item.Day == date.DayOfWeek)
            .Select(item => item.Window)
            .ToArray();

        long stepTicks = TimeSpan.FromMinutes(policy.StartIntervalMinutes).Ticks;
        Dictionary<long, AvailableSlot> starts = new();

        foreach (LocalTimeWindow window in windows)
        {
            long ticks = ((window.Start.Ticks + stepTicks - 1) / stepTicks) * stepTicks;
            while (ticks < window.End.Ticks)
            {
                DateTime localStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromTicks(ticks))), DateTimeKind.Unspecified);
                foreach (DateTimeOffset start in ResolveLocalStart(localStart))
                {
                    if (FitsInsideWindow(start, date, window, durationMinutes))
                    {
                        AvailableSlot slot = new(start, durationMinutes);
                        starts.TryAdd(slot.StartsAtUtc.UtcDateTime.Ticks, slot);
                    }
                }

                ticks += stepTicks;
            }
        }

        return starts.Values.OrderBy(slot => slot.StartsAtUtc).ToArray();
    }

    /// <summary>Maps a local time to valid offsets, including repeated clock-change times.</summary>
    private IEnumerable<DateTimeOffset> ResolveLocalStart(DateTime localStart)
    {
        TimeZoneInfo timeZone = policy.BusinessTimeZone;
        if (timeZone.IsInvalidTime(localStart))
        {
            return [];
        }

        if (timeZone.IsAmbiguousTime(localStart))
        {
            return timeZone.GetAmbiguousTimeOffsets(localStart)
                .Select(offset => new DateTimeOffset(localStart, offset));
        }

        return [new DateTimeOffset(localStart, timeZone.GetUtcOffset(localStart))];
    }

    /// <summary>Checks each session minute against its local date and availability window.</summary>
    private bool FitsInsideWindow(DateTimeOffset startsAt, DateOnly date, LocalTimeWindow window, int durationMinutes)
    {
        DateTimeOffset end = startsAt.AddMinutes(durationMinutes);
        for (DateTimeOffset instant = startsAt; instant < end; instant = instant.AddMinutes(1))
        {
            DateTimeOffset local = TimeZoneInfo.ConvertTime(instant, policy.BusinessTimeZone);
            if (DateOnly.FromDateTime(local.DateTime) != date ||
                TimeOnly.FromDateTime(local.DateTime) < window.Start ||
                TimeOnly.FromDateTime(local.DateTime) >= window.End)
            {
                return false;
            }
        }

        return true;
    }
}
