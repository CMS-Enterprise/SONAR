using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPermissionUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_permission_user_id",
                table: "user_permission");

            migrationBuilder.CreateIndex(
                name: "ix_user_permission_unique_environment_scope",
                table: "user_permission",
                columns: new[] { "user_id", "environment_id", "permission" },
                unique: true,
                filter: "environment_id IS NOT NULL AND tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_permission_unique_global_scope",
                table: "user_permission",
                columns: new[] { "user_id", "permission" },
                unique: true,
                filter: "environment_id IS NULL AND tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_permission_unique_tenant_scope",
                table: "user_permission",
                columns: new[] { "user_id", "environment_id", "tenant_id", "permission" },
                unique: true,
                filter: "environment_id IS NOT NULL AND tenant_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_permission_unique_environment_scope",
                table: "user_permission");

            migrationBuilder.DropIndex(
                name: "ix_user_permission_unique_global_scope",
                table: "user_permission");

            migrationBuilder.DropIndex(
                name: "ix_user_permission_unique_tenant_scope",
                table: "user_permission");

            migrationBuilder.CreateIndex(
                name: "ix_user_permission_user_id",
                table: "user_permission",
                column: "user_id");
        }
    }
}
