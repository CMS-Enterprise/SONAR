using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorDetailEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "error_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_name = table.Column<string>(type: "text", nullable: true, collation: "ci_collation"),
                    health_check_name = table.Column<string>(type: "text", nullable: true, collation: "ci_collation"),
                    level = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    configuration = table.Column<string>(type: "text", nullable: true, collation: "ci_collation"),
                    stack_trace = table.Column<string>(type: "text", nullable: true, collation: "ci_collation")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_error_detail", x => x.id);
                    table.ForeignKey(
                        name: "fk_error_detail_environment_environment_id",
                        column: x => x.environment_id,
                        principalTable: "environment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_error_detail_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_error_detail_environment_id",
                table: "error_detail",
                column: "environment_id");

            migrationBuilder.CreateIndex(
                name: "ix_error_detail_tenant_id",
                table: "error_detail",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "error_detail");
        }
    }
}
