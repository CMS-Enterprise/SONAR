using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations {
  /// <inheritdoc />
  public partial class ErrorReportCascadeDeletes : Migration {
    /// <inheritdoc />
    /// <remarks>
    /// Manually recreates the foreign key constraints on ErrorReport with the correct delete behavior.
    /// </remarks>
    protected override void Up(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropForeignKey(
        "fk_error_report_environment_environment_id",
        "error_report");

      migrationBuilder.DropForeignKey(
        "fk_error_report_tenant_tenant_id",
        "error_report");

      migrationBuilder.AddForeignKey(
        name: "fk_error_report_environment_environment_id",
        table: "error_report",
        column: "environment_id",
        principalTable: "environment",
        principalColumn: "id",
        onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
        name: "fk_error_report_tenant_tenant_id",
        table: "error_report",
        column: "tenant_id",
        principalTable: "tenant",
        principalColumn: "id",
        onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropForeignKey(
        "fk_error_report_environment_environment_id",
        "error_report");

      migrationBuilder.DropForeignKey(
        "fk_error_report_tenant_tenant_id",
        "error_report");

      migrationBuilder.AddForeignKey(
        name: "fk_error_report_environment_environment_id",
        table: "error_report",
        column: "environment_id",
        principalTable: "environment",
        principalColumn: "id");

      migrationBuilder.AddForeignKey(
        name: "fk_error_report_tenant_tenant_id",
        table: "error_report",
        column: "tenant_id",
        principalTable: "tenant",
        principalColumn: "id");
    }
  }
}
