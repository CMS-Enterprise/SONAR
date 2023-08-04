using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeysUsageColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "creation",
                table: "api_key",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "last_usage",
                table: "api_key",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "creation",
                table: "api_key");

            migrationBuilder.DropColumn(
                name: "last_usage",
                table: "api_key");
        }
    }
}
