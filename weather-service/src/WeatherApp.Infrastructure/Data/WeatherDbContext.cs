using Microsoft.EntityFrameworkCore;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Data;

public sealed class WeatherDbContext(DbContextOptions<WeatherDbContext> options) : DbContext(options)
{
    public DbSet<WeatherObservation> WeatherObservations => Set<WeatherObservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherObservation>(builder =>
        {
            builder.HasKey(observation => observation.Id);
            builder.Property(observation => observation.Location).HasMaxLength(150).IsRequired();
            builder.Property(observation => observation.MetricType).HasMaxLength(80).IsRequired();
            builder.Property(observation => observation.MetricValue).HasPrecision(10, 2);
            builder.Property(observation => observation.MetricUnit).HasMaxLength(30).IsRequired();
            builder.Property(observation => observation.Provider).HasMaxLength(80).IsRequired();
            builder.Property(observation => observation.ProviderDataset).HasMaxLength(120).IsRequired();
        });
    }
}
