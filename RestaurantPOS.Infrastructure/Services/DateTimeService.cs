using RestaurantPOS.Domain.Interfaces;

namespace RestaurantPOS.Infrastructure.Services;

/// <summary>Provides the current UTC and local date/time.</summary>
public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local);
}
