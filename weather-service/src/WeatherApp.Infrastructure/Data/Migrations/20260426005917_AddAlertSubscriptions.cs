using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ThresholdValue = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscriptions_Email_Location_AlertType_IsActive",
                table: "AlertSubscriptions",
                columns: new[] { "Email", "Location", "AlertType", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertSubscriptions");
        }
    }
}
