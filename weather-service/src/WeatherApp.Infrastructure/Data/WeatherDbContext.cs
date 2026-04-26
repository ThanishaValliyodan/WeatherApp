using Microsoft.EntityFrameworkCore;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Data;

public sealed class WeatherDbContext(DbContextOptions<WeatherDbContext> options) : DbContext(options)
{
    public DbSet<WeatherObservation> WeatherObservations => Set<WeatherObservation>();
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
    public DbSet<WeatherLocation> WeatherLocations => Set<WeatherLocation>();
    public DbSet<AlertSubscription> AlertSubscriptions => Set<AlertSubscription>();
    public DbSet<WeatherSyncRun> WeatherSyncRuns => Set<WeatherSyncRun>();
    public DbSet<WeatherSyncCheckpoint> WeatherSyncCheckpoints => Set<WeatherSyncCheckpoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherObservation>(builder =>
        {
            builder.HasKey(observation => observation.Id);
            builder.Property(observation => observation.Location).HasMaxLength(150).IsRequired();
            builder.Property(observation => observation.LocationType).HasMaxLength(80).IsRequired();
            builder.Property(observation => observation.StationId).HasMaxLength(50);
            builder.Property(observation => observation.StationName).HasMaxLength(150);
            builder.Property(observation => observation.Region).HasMaxLength(80);
            builder.Property(observation => observation.Latitude).HasPrecision(9, 6);
            builder.Property(observation => observation.Longitude).HasPrecision(9, 6);
            builder.Property(observation => observation.MetricType).HasMaxLength(80).IsRequired();
            builder.Property(observation => observation.MetricValue).HasPrecision(10, 2);
            builder.Property(observation => observation.MetricUnit).HasMaxLength(30).IsRequired();
            builder.Property(observation => observation.TemperatureCelsius).HasPrecision(10, 2);
            builder.Property(observation => observation.HumidityPercent).HasPrecision(10, 2);
            builder.Property(observation => observation.RainfallMm).HasPrecision(10, 2);
            builder.Property(observation => observation.WindSpeed).HasPrecision(10, 2);
            builder.Property(observation => observation.WindDirectionDegrees).HasPrecision(10, 2);
            builder.Property(observation => observation.AirQualityIndex).HasPrecision(10, 2);
            builder.Property(observation => observation.Summary).HasMaxLength(300);
            builder.Property(observation => observation.Provider).HasMaxLength(80).IsRequired();
            builder.Property(observation => observation.ProviderDataset).HasMaxLength(120).IsRequired();
            builder.HasIndex(observation => new
            {
                observation.Provider,
                observation.ProviderDataset,
                observation.Location,
                observation.StationId,
                observation.MetricType,
                observation.ObservationTimeUtc
            }).IsUnique();
        });

        modelBuilder.Entity<WeatherForecast>(builder =>
        {
            builder.HasKey(forecast => forecast.Id);
            builder.Property(forecast => forecast.Location).HasMaxLength(150).IsRequired();
            builder.Property(forecast => forecast.LocationType).HasMaxLength(80).IsRequired();
            builder.Property(forecast => forecast.Area).HasMaxLength(150);
            builder.Property(forecast => forecast.Region).HasMaxLength(80);
            builder.Property(forecast => forecast.Latitude).HasPrecision(9, 6);
            builder.Property(forecast => forecast.Longitude).HasPrecision(9, 6);
            builder.Property(forecast => forecast.ForecastType).HasMaxLength(80).IsRequired();
            builder.Property(forecast => forecast.TemperatureLowCelsius).HasPrecision(10, 2);
            builder.Property(forecast => forecast.TemperatureHighCelsius).HasPrecision(10, 2);
            builder.Property(forecast => forecast.HumidityLowPercent).HasPrecision(10, 2);
            builder.Property(forecast => forecast.HumidityHighPercent).HasPrecision(10, 2);
            builder.Property(forecast => forecast.WindSpeedLow).HasPrecision(10, 2);
            builder.Property(forecast => forecast.WindSpeedHigh).HasPrecision(10, 2);
            builder.Property(forecast => forecast.WindDirection).HasMaxLength(80);
            builder.Property(forecast => forecast.ForecastCode).HasMaxLength(80);
            builder.Property(forecast => forecast.Summary).HasMaxLength(300).IsRequired();
            builder.Property(forecast => forecast.Provider).HasMaxLength(80).IsRequired();
            builder.Property(forecast => forecast.ProviderDataset).HasMaxLength(120).IsRequired();
            builder.HasIndex(forecast => new
            {
                forecast.Provider,
                forecast.ProviderDataset,
                forecast.Location,
                forecast.ForecastType,
                forecast.ForecastTimeUtc,
                forecast.ValidFromUtc
            }).IsUnique();
        });

        modelBuilder.Entity<WeatherLocation>(builder =>
        {
            builder.HasKey(location => location.Id);
            builder.Property(location => location.Name).HasMaxLength(150).IsRequired();
            builder.Property(location => location.LocationType).HasMaxLength(80).IsRequired();
            builder.Property(location => location.StationId).HasMaxLength(50);
            builder.Property(location => location.Region).HasMaxLength(80);
            builder.Property(location => location.Latitude).HasPrecision(9, 6);
            builder.Property(location => location.Longitude).HasPrecision(9, 6);
            builder.Property(location => location.SourceDataset).HasMaxLength(120).IsRequired();
            builder.HasIndex(location => new { location.Name, location.LocationType, location.StationId }).IsUnique();
        });

        modelBuilder.Entity<AlertSubscription>(builder =>
        {
            builder.HasKey(subscription => subscription.Id);
            builder.Property(subscription => subscription.Email).HasMaxLength(254).IsRequired();
            builder.Property(subscription => subscription.Location).HasMaxLength(150).IsRequired();
            builder.Property(subscription => subscription.AlertType).HasMaxLength(80).IsRequired();
            builder.Property(subscription => subscription.ThresholdValue).HasPrecision(10, 2);
            builder.HasIndex(subscription => new { subscription.Email, subscription.Location, subscription.AlertType, subscription.IsActive });
        });

        modelBuilder.Entity<WeatherSyncRun>(builder =>
        {
            builder.HasKey(run => run.Id);
            builder.Property(run => run.SyncType).HasMaxLength(80).IsRequired();
            builder.Property(run => run.Status).HasMaxLength(80).IsRequired();
            builder.Property(run => run.ErrorMessage).HasMaxLength(1000);
        });

        modelBuilder.Entity<WeatherSyncCheckpoint>(builder =>
        {
            builder.HasKey(checkpoint => checkpoint.Id);
            builder.Property(checkpoint => checkpoint.Provider).HasMaxLength(80).IsRequired();
            builder.Property(checkpoint => checkpoint.ProviderDataset).HasMaxLength(120).IsRequired();
            builder.Property(checkpoint => checkpoint.LocationScope).HasMaxLength(120).IsRequired();
            builder.Property(checkpoint => checkpoint.Status).HasMaxLength(80).IsRequired();
            builder.Property(checkpoint => checkpoint.Checksum).HasMaxLength(120);
            builder.Property(checkpoint => checkpoint.ErrorMessage).HasMaxLength(1000);
            builder.HasIndex(checkpoint => new
            {
                checkpoint.Provider,
                checkpoint.ProviderDataset,
                checkpoint.LocationScope,
                checkpoint.DataDate
            }).IsUnique();
        });
    }
}
