using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameErrorDetailEntityToErrorReport : Migration
    {
      /// <inheritdoc />
      protected override void Up(MigrationBuilder migrationBuilder) {

        migrationBuilder.DropPrimaryKey(
          name: "pk_error_detail",
          table: "error_detail");

        migrationBuilder.DropForeignKey(
          name: "fk_error_detail_environment_environment_id",
          table: "error_detail");

        migrationBuilder.DropForeignKey(
          name: "fk_error_detail_tenant_tenant_id",
          table: "error_detail");

        migrationBuilder.RenameTable(
          name: "error_detail",
          newName: "error_report");

        migrationBuilder.RenameIndex(
          name: "ix_error_detail_environment_id",
          newName: "ix_error_report_environment_id");

        migrationBuilder.RenameIndex(
          name: "ix_error_detail_tenant_id",
          newName: "ix_error_report_tenant_id");

        migrationBuilder.AddPrimaryKey(
          name: "pk_error_report",
          table: "error_report",
          column: "id");

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

      /// <inheritdoc />
      protected override void Down(MigrationBuilder migrationBuilder)
      {
        migrationBuilder.DropPrimaryKey(
          name: "pk_error_report",
          table: "error_report");

        migrationBuilder.DropForeignKey(
          name: "fk_error_report_environment_environment_id",
          table: "error_report");

        migrationBuilder.DropForeignKey(
          name: "fk_error_report_tenant_tenant_id",
          table: "error_report");

        migrationBuilder.RenameTable(
          name: "error_report",
          newName: "error_detail");

        migrationBuilder.RenameIndex(
          name: "ix_error_report_environment_id",
          newName: "ix_error_detail_environment_id");

        migrationBuilder.RenameIndex(
          name: "ix_error_report_tenant_id",
          newName: "ix_error_detail_tenant_id");

        migrationBuilder.AddPrimaryKey(
          name: "pk_error_detail",
          table: "error_detail",
          column: "id");

        migrationBuilder.AddForeignKey(
          name: "fk_error_detail_environment_environment_id",
          table: "error_detail",
          column: "environment_id",
          principalTable: "environment",
          principalColumn: "id");

        migrationBuilder.AddForeignKey(
          name: "fk_error_detail_tenant_tenant_id",
          table: "error_detail",
          column: "tenant_id",
          principalTable: "tenant",
          principalColumn: "id");
      }
    }
}
