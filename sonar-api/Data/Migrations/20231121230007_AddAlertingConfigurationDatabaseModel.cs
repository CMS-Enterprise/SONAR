using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertingConfigurationDatabaseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alert_receiver",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "ci_collation"),
                    type = table.Column<int>(type: "integer", nullable: false),
                    options = table.Column<string>(type: "text", nullable: false, collation: "ci_collation")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alert_receiver", x => x.id);
                    table.ForeignKey(
                        name: "fk_alert_receiver_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alerting_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_receiver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "ci_collation"),
                    threshold = table.Column<int>(type: "integer", nullable: false),
                    delay = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerting_rule", x => x.id);
                    table.ForeignKey(
                        name: "fk_alerting_rule_alert_receiver_alert_receiver_id",
                        column: x => x.alert_receiver_id,
                        principalTable: "alert_receiver",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_alerting_rule_service_service_id",
                        column: x => x.service_id,
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alert_receiver_tenant_id_name",
                table: "alert_receiver",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_alerting_rule_alert_receiver_id",
                table: "alerting_rule",
                column: "alert_receiver_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerting_rule_service_id_name",
                table: "alerting_rule",
                columns: new[] { "service_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerting_rule");

            migrationBuilder.DropTable(
                name: "alert_receiver");
        }
    }
}
