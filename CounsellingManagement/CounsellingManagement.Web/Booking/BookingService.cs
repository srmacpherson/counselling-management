namespace CounsellingManagement.Web.Booking;

/// <summary>Filters scheduled slots through booking limits and existing reservations.</summary>
public sealed class BookingService(AvailabilityService availabilityService, BookingPolicy policy)
{
    /// <summary>Returns slots that satisfy the schedule, horizon, notice, and conflict rules.</summary>
    public IReadOnlyList<AvailableSlot> GetAvailableSlots(
        AvailabilitySchedule schedule,
        DateOnly date,
        int durationMinutes,
        IEnumerable<Appointment> appointments,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(appointments);

        if (!IsWithinBookingWindow(date, now) || !policy.OfferedSessionDurationsMinutes.Contains(durationMinutes))
        {
            return [];
        }

        Appointment[] existingAppointments = appointments.ToArray();
        DateTimeOffset earliestStart = now.ToUniversalTime().Add(policy.MinimumNotice);

        return availabilityService.GetScheduledSlots(schedule, date, durationMinutes)
            .Where(slot => slot.StartsAtUtc >= earliestStart)
            .Where(slot => !ConflictsWithUnavailablePeriod(slot, schedule.UnavailablePeriods))
            .Where(slot => !ConflictsWithAppointment(slot, existingAppointments))
            .ToArray();
    }

    /// <summary>Validates a requested start and reports the first rule it violates.</summary>
    public BookingValidationResult ValidateBooking(
        AvailabilitySchedule schedule,
        DateTimeOffset startsAt,
        int durationMinutes,
        IEnumerable<Appointment> appointments,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(appointments);

        if (!policy.OfferedSessionDurationsMinutes.Contains(durationMinutes))
        {
            return BookingValidationResult.Invalid(BookingValidationError.SessionDurationNotOffered);
        }

        DateOnly localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(startsAt, policy.BusinessTimeZone).DateTime);
        if (!IsWithinBookingWindow(localDate, now))
        {
            return BookingValidationResult.Invalid(BookingValidationError.OutsideBookingWindow);
        }

        if (startsAt.ToUniversalTime() < now.ToUniversalTime().Add(policy.MinimumNotice))
        {
            return BookingValidationResult.Invalid(BookingValidationError.MinimumNoticeNotMet);
        }

        AvailableSlot slot = new(startsAt, durationMinutes);
        bool scheduledSlot = availabilityService.GetScheduledSlots(schedule, localDate, durationMinutes)
            .Any(candidate => candidate.StartsAtUtc == slot.StartsAtUtc);
        if (!scheduledSlot)
        {
            return BookingValidationResult.Invalid(BookingValidationError.StartTimeNotAvailable);
        }

        if (ConflictsWithUnavailablePeriod(slot, schedule.UnavailablePeriods))
        {
            return BookingValidationResult.Invalid(BookingValidationError.UnavailablePeriodConflict);
        }

        Appointment[] existingAppointments = appointments.ToArray();
        if (ConflictsWithAppointment(slot, existingAppointments))
        {
            return BookingValidationResult.Invalid(BookingValidationError.AppointmentConflict);
        }

        return BookingValidationResult.Valid;
    }

    /// <summary>Checks a local date against today and the configured booking horizon.</summary>
    private bool IsWithinBookingWindow(DateOnly date, DateTimeOffset now)
    {
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(now, policy.BusinessTimeZone);
        DateOnly today = DateOnly.FromDateTime(localNow.DateTime);
        return date >= today && date <= today.AddDays(policy.BookingHorizonDays);
    }

    /// <summary>Checks whether a proposed slot overlaps a counsellor block-out.</summary>
    private static bool ConflictsWithUnavailablePeriod(AvailableSlot slot, IEnumerable<UnavailablePeriod> periods) =>
        periods.Any(period => Overlaps(slot.StartsAtUtc, slot.EndsAtUtc, period.StartsAtUtc, period.EndsAtUtc));

    /// <summary>Checks confirmed appointments, applying the configured buffer.</summary>
    private bool ConflictsWithAppointment(AvailableSlot slot, IEnumerable<Appointment> appointments) =>
        appointments.Any(appointment =>
            appointment.Status != AppointmentStatus.Cancelled &&
            Overlaps(
                slot.StartsAtUtc,
                slot.EndsAtUtc,
                appointment.StartsAtUtc.Subtract(policy.Buffer),
                appointment.EndsAtUtc.Add(policy.Buffer)));

    /// <summary>Tests whether two half-open time ranges overlap.</summary>
    private static bool Overlaps(DateTimeOffset firstStart, DateTimeOffset firstEnd, DateTimeOffset secondStart, DateTimeOffset secondEnd) =>
        firstStart < secondEnd && firstEnd > secondStart;
}
