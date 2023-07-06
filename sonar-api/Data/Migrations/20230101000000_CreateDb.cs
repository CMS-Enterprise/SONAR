using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          // This is a blank migration that only creates the databse but makes no other schema changes;
          // It's only used in development environments with the migratedb --re-create command, to ensure a blank
          // database exists with the migration history table before running the migration service which needs the
          // history table available to lock.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
