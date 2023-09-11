using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceVersionCacheEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_version_cache",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    tenant = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    service = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    version_check_type = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_version_cache", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_version_cache_environment_tenant_service_version_ch",
                table: "service_version_cache",
                columns: new[] { "environment", "tenant", "service", "version_check_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_version_cache");
        }
    }
}
