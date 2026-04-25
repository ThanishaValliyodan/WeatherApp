using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LocationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    ForecastTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValidToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ForecastType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TemperatureLowCelsius = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    TemperatureHighCelsius = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    HumidityLowPercent = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    HumidityHighPercent = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    WindSpeedLow = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    WindSpeedHigh = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    WindDirection = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ForecastCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ProviderDataset = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RawProviderPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LocationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    SourceDataset = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LocationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StationName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    MetricType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MetricValue = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MetricUnit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ObservationTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TemperatureCelsius = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    HumidityPercent = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    RainfallMm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    WindSpeed = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    WindDirectionDegrees = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    AirQualityIndex = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ProviderDataset = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RawProviderPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherObservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherSyncCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ProviderDataset = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LocationScope = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DataDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RecordsInserted = table.Column<int>(type: "int", nullable: false),
                    Checksum = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherSyncCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherSyncRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SyncType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RequestedFromDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RequestedToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalDatesChecked = table.Column<int>(type: "int", nullable: false),
                    TotalDatesSkipped = table.Column<int>(type: "int", nullable: false),
                    TotalDatesFetched = table.Column<int>(type: "int", nullable: false),
                    TotalRecordsInserted = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherSyncRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherForecasts_Provider_ProviderDataset_Location_ForecastType_ForecastTimeUtc_ValidFromUtc",
                table: "WeatherForecasts",
                columns: new[] { "Provider", "ProviderDataset", "Location", "ForecastType", "ForecastTimeUtc", "ValidFromUtc" },
                unique: true,
                filter: "[ValidFromUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherLocations_Name_LocationType_StationId",
                table: "WeatherLocations",
                columns: new[] { "Name", "LocationType", "StationId" },
                unique: true,
                filter: "[StationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherObservations_Provider_ProviderDataset_Location_StationId_MetricType_ObservationTimeUtc",
                table: "WeatherObservations",
                columns: new[] { "Provider", "ProviderDataset", "Location", "StationId", "MetricType", "ObservationTimeUtc" },
                unique: true,
                filter: "[StationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherSyncCheckpoints_Provider_ProviderDataset_LocationScope_DataDate",
                table: "WeatherSyncCheckpoints",
                columns: new[] { "Provider", "ProviderDataset", "LocationScope", "DataDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherForecasts");

            migrationBuilder.DropTable(
                name: "WeatherLocations");

            migrationBuilder.DropTable(
                name: "WeatherObservations");

            migrationBuilder.DropTable(
                name: "WeatherSyncCheckpoints");

            migrationBuilder.DropTable(
                name: "WeatherSyncRuns");
        }
    }
}
