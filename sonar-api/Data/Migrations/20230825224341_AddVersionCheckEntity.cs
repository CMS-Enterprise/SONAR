using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionCheckEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "version_check",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_check_type = table.Column<int>(type: "integer", nullable: false),
                    definition = table.Column<string>(type: "text", nullable: false, collation: "ci_collation")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_version_check", x => x.id);
                    table.ForeignKey(
                        name: "fk_version_check_service_service_id",
                        column: x => x.service_id,
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_version_check_service_id_version_check_type",
                table: "version_check",
                columns: new[] { "service_id", "version_check_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "version_check");
        }
    }
}
