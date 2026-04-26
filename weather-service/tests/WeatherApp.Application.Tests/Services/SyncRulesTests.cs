using WeatherApp.Application.DTOs;
using WeatherApp.Application.Services;
using Xunit;

namespace WeatherApp.Application.Tests.Services;

public sealed class SyncRulesTests
{
    [Fact]
    public void ValidateRequest_AllowsSingleDateRange()
    {
        var date = new DateOnly(2026, 4, 26);
        var request = new SyncRequest(date, date, Force: false);

        SyncRules.ValidateRequest(request);
    }

    [Fact]
    public void ValidateRequest_AllowsValidDateRange()
    {
        var request = new SyncRequest(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 26),
            Force: true);

        SyncRules.ValidateRequest(request);
    }

    [Fact]
    public void ValidateRequest_RejectsFromDateAfterToDate()
    {
        var request = new SyncRequest(
            new DateOnly(2026, 4, 26),
            new DateOnly(2026, 4, 1),
            Force: false);

        var exception = Assert.Throws<ArgumentException>(() => SyncRules.ValidateRequest(request));
        Assert.Equal("from must not be later than to.", exception.Message);
    }
}
