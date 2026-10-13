using CounsellingManagement.Web.Booking;
using Xunit;

namespace CounsellingManagement.Tests;

/// <summary>Tests supported session lengths and booking-policy configuration.</summary>
public class BookingPolicyTests
{
    /// <summary>Checks that only supported session lengths pass validation.</summary>
    [Theory]
    [InlineData(15, true)]
    [InlineData(30, true)]
    [InlineData(45, true)]
    [InlineData(60, true)]
    [InlineData(90, true)]
    [InlineData(10, false)]
    [InlineData(20, false)]
    [InlineData(75, false)]
    [InlineData(95, false)]
    public void IsValidSessionDuration_OnlyAllowsSupportedLengths(int durationMinutes, bool expected)
    {
        Assert.Equal(expected, BookingPolicy.IsValidSessionDuration(durationMinutes));
    }

    /// <summary>Rejects a policy containing an unsupported session length.</summary>
    [Fact]
    public void Constructor_RejectsInvalidOfferedDuration()
    {
        Assert.Throws<ArgumentException>(() => new BookingPolicy(offeredSessionDurationsMinutes: [20]));
    }

    /// <summary>Allows the counsellor to offer a subset of supported lengths.</summary>
    [Fact]
    public void Constructor_AllowsCounsellorToOfferASubsetOfSupportedDurations()
    {
        BookingPolicy policy = new(offeredSessionDurationsMinutes: [30, 60]);

        Assert.Equal([30, 60], policy.OfferedSessionDurationsMinutes);
    }
}
