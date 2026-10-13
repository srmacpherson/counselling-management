namespace CounsellingManagement.Web.Booking;

/// <summary>Identifies why a requested appointment cannot be booked.</summary>
public enum BookingValidationError
{
    None,
    SessionDurationNotOffered,
    OutsideBookingWindow,
    MinimumNoticeNotMet,
    StartTimeNotAvailable,
    UnavailablePeriodConflict,
    AppointmentConflict
}
