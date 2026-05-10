using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MH.Capstone.Domain.Migrations
{
    /// <inheritdoc />
    public partial class FixSightingUserIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AlterColumn generates DROP/CREATE INDEX for IX_Sighting_UserId, but that index
            // was only present on DBs created from a since-deleted migration. Use raw SQL so
            // the drop is conditional and the migration applies cleanly on a fresh DB.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_Sighting_UserId'
                      AND object_id = OBJECT_ID(N'[Sighting]')
                )
                    DROP INDEX [IX_Sighting_UserId] ON [Sighting];

                DECLARE @var sysname;
                SELECT @var = [d].[name]
                FROM [sys].[default_constraints] [d]
                INNER JOIN [sys].[columns] [c]
                    ON [d].[parent_column_id] = [c].[column_id]
                    AND [d].[parent_object_id] = [c].[object_id]
                WHERE [d].[parent_object_id] = OBJECT_ID(N'[Sighting]')
                  AND [c].[name] = N'UserId';
                IF @var IS NOT NULL EXEC(N'ALTER TABLE [Sighting] DROP CONSTRAINT [' + @var + '];');

                ALTER TABLE [Sighting] ALTER COLUMN [UserId] nvarchar(450) NOT NULL;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_Sighting_UserId'
                      AND object_id = OBJECT_ID(N'[Sighting]')
                )
                    CREATE INDEX [IX_Sighting_UserId] ON [Sighting] ([UserId]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                    name: "UserId",
                    table: "Sighting",
                    type: "uniqueidentifier",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(450)",
                    oldMaxLength: 450);
        }
    }
}
