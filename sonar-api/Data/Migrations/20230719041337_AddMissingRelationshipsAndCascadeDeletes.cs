using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingRelationshipsAndCascadeDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_api_key_tenant_tenant_id",
                table: "api_key");

            migrationBuilder.DropForeignKey(
                name: "fk_user_permission_environment_environment_id",
                table: "user_permission");

            migrationBuilder.DropForeignKey(
                name: "fk_user_permission_tenant_tenant_id",
                table: "user_permission");

            migrationBuilder.CreateIndex(
                name: "ix_api_key_environment_id",
                table: "api_key",
                column: "environment_id");

            migrationBuilder.AddForeignKey(
                name: "fk_api_key_environment_environment_id",
                table: "api_key",
                column: "environment_id",
                principalTable: "environment",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_api_key_tenant_tenant_id",
                table: "api_key",
                column: "tenant_id",
                principalTable: "tenant",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_permission_environment_environment_id",
                table: "user_permission",
                column: "environment_id",
                principalTable: "environment",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_permission_tenant_tenant_id",
                table: "user_permission",
                column: "tenant_id",
                principalTable: "tenant",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_api_key_environment_environment_id",
                table: "api_key");

            migrationBuilder.DropForeignKey(
                name: "fk_api_key_tenant_tenant_id",
                table: "api_key");

            migrationBuilder.DropForeignKey(
                name: "fk_user_permission_environment_environment_id",
                table: "user_permission");

            migrationBuilder.DropForeignKey(
                name: "fk_user_permission_tenant_tenant_id",
                table: "user_permission");

            migrationBuilder.DropIndex(
                name: "ix_api_key_environment_id",
                table: "api_key");

            migrationBuilder.AddForeignKey(
                name: "fk_api_key_tenant_tenant_id",
                table: "api_key",
                column: "tenant_id",
                principalTable: "tenant",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_permission_environment_environment_id",
                table: "user_permission",
                column: "environment_id",
                principalTable: "environment",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_permission_tenant_tenant_id",
                table: "user_permission",
                column: "tenant_id",
                principalTable: "tenant",
                principalColumn: "id");
        }
    }
}
