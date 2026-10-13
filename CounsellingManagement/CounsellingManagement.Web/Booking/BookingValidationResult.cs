namespace CounsellingManagement.Web.Booking;

/// <summary>Contains the result and failure reason for a booking check.</summary>
public sealed record BookingValidationResult(bool IsValid, BookingValidationError Error)
{
    public static BookingValidationResult Valid { get; } = new(true, BookingValidationError.None);

    /// <summary>Creates a failed result with the applicable validation error.</summary>
    public static BookingValidationResult Invalid(BookingValidationError error) =>
        new(false, error);
}
