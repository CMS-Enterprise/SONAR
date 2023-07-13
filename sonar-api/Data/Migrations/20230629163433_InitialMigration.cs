using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "environment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "ci_collation")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_environment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_health_cache",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    tenant = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    service = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    aggregate_status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_health_cache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false, collation: "ci_collation")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_environment_environment_id",
                        column: x => x.environment_id,
                        principalTable: "environment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "health_check_cache",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_health_id = table.Column<Guid>(type: "uuid", nullable: false),
                    health_check = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_check_cache", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_check_cache_service_health_cache_service_health_cach",
                        column: x => x.service_health_id,
                        principalTable: "service_health_cache",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "api_key",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(74)", maxLength: 74, nullable: false, collation: "ci_collation"),
                    type = table.Column<int>(type: "integer", nullable: false),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_key", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_key_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "service",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "ci_collation"),
                    display_name = table.Column<string>(type: "text", nullable: false, collation: "ci_collation"),
                    description = table.Column<string>(type: "text", nullable: true, collation: "ci_collation"),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    is_root_service = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "health_check",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "ci_collation"),
                    description = table.Column<string>(type: "text", nullable: true, collation: "ci_collation"),
                    type = table.Column<int>(type: "integer", nullable: false),
                    definition = table.Column<string>(type: "text", nullable: false, collation: "ci_collation")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_check", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_check_service_service_id",
                        column: x => x.service_id,
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_relationship",
                columns: table => new
                {
                    parent_service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_relationship", x => new { x.service_id, x.parent_service_id });
                    table.ForeignKey(
                        name: "fk_service_relationship_service_service_id",
                        column: x => x.service_id,
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_service_relationship_service_service_id1",
                        column: x => x.parent_service_id,
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_key_key",
                table: "api_key",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "ix_api_key_tenant_id",
                table: "api_key",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_environment_name",
                table: "environment",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_health_check_service_id_name",
                table: "health_check",
                columns: new[] { "service_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_health_check_cache_service_health_id_health_check",
                table: "health_check_cache",
                columns: new[] { "service_health_id", "health_check" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_tenant_id_name",
                table: "service",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_health_cache_environment_tenant_service",
                table: "service_health_cache",
                columns: new[] { "environment", "tenant", "service" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_relationship_parent_service_id",
                table: "service_relationship",
                column: "parent_service_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_environment_id_name",
                table: "tenant",
                columns: new[] { "environment_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_key");

            migrationBuilder.DropTable(
                name: "health_check");

            migrationBuilder.DropTable(
                name: "health_check_cache");

            migrationBuilder.DropTable(
                name: "service_relationship");

            migrationBuilder.DropTable(
                name: "service_health_cache");

            migrationBuilder.DropTable(
                name: "service");

            migrationBuilder.DropTable(
                name: "tenant");

            migrationBuilder.DropTable(
                name: "environment");
        }
    }
}
