using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ad_hoc_maintenance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_recording = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_recorded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    discriminator = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_hoc_maintenance", x => x.id);
                    table.ForeignKey(
                        name: "fk_ad_hoc_maintenance_environment_environment_id",
                        column: x => x.environment_id,
                        principalTable: "environment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ad_hoc_maintenance_service_service_id",
                        column: x => x.service_id,
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ad_hoc_maintenance_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ad_hoc_maintenance_user_applied_by_user_id",
                        column: x => x.applied_by_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_maintenance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_expression = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    schedule_time_zone = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    is_recording = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_recorded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    discriminator = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheduled_maintenance", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheduled_maintenance_environment_environment_id",
                        column: x => x.environment_id,
                        principalTable: "environment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scheduled_maintenance_service_service_id",
                        column: x => x.service_id,
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scheduled_maintenance_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ad_hoc_maintenance_applied_by_user_id",
                table: "ad_hoc_maintenance",
                column: "applied_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ad_hoc_maintenance_environment_id",
                table: "ad_hoc_maintenance",
                column: "environment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ad_hoc_maintenance_service_id",
                table: "ad_hoc_maintenance",
                column: "service_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ad_hoc_maintenance_tenant_id",
                table: "ad_hoc_maintenance",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_maintenance_environment_id",
                table: "scheduled_maintenance",
                column: "environment_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_maintenance_service_id",
                table: "scheduled_maintenance",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_maintenance_tenant_id",
                table: "scheduled_maintenance",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ad_hoc_maintenance");

            migrationBuilder.DropTable(
                name: "scheduled_maintenance");
        }
    }
}
