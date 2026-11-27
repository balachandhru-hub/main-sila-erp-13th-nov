using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Aligns Phase 2A Event/Menu tables with BaseModel audit column names (date_created / date_updated).</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261129170000_SilaCulinaryPhase2AColumnSync")]
    public partial class SilaCulinaryPhase2AColumnSyncMigration : Migration
    {
        private static readonly string[] Phase2ATables =
        {
            "sila_culinary_event",
            "sila_culinary_event_session",
            "sila_culinary_event_template",
            "sila_culinary_menu",
            "sila_culinary_menu_line",
            "sila_culinary_event_revision"
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (string table in Phase2ATables)
            {
                migrationBuilder.Sql($@"
IF OBJECT_ID(N'[buyersystem].[{table}]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'buyersystem.{table}', N'created_on') IS NOT NULL AND COL_LENGTH(N'buyersystem.{table}', N'date_created') IS NULL
        EXEC sp_rename N'buyersystem.{table}.created_on', N'date_created', N'COLUMN';

    IF COL_LENGTH(N'buyersystem.{table}', N'updated_on') IS NOT NULL AND COL_LENGTH(N'buyersystem.{table}', N'date_updated') IS NULL
        EXEC sp_rename N'buyersystem.{table}.updated_on', N'date_updated', N'COLUMN';
END");

                migrationBuilder.Sql($@"
IF OBJECT_ID(N'[buyersystem].[{table}]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'buyersystem.{table}', N'date_updated') IS NOT NULL
    BEGIN
        EXEC(N'UPDATE [buyersystem].[{table}] SET [date_updated] = COALESCE([date_updated], [date_created], GETUTCDATE()) WHERE [date_updated] IS NULL;');
        EXEC(N'ALTER TABLE [buyersystem].[{table}] ALTER COLUMN [date_updated] datetime2 NOT NULL;');
    END

    IF COL_LENGTH(N'buyersystem.{table}', N'created_by') IS NOT NULL
    BEGIN
        EXEC(N'UPDATE [buyersystem].[{table}] SET [created_by] = ''00000000-0000-0000-0000-000000000000'' WHERE [created_by] IS NULL;');
        EXEC(N'ALTER TABLE [buyersystem].[{table}] ALTER COLUMN [created_by] uniqueidentifier NOT NULL;');
    END

    IF COL_LENGTH(N'buyersystem.{table}', N'updated_by') IS NOT NULL
    BEGIN
        EXEC(N'UPDATE [buyersystem].[{table}] SET [updated_by] = ''00000000-0000-0000-0000-000000000000'' WHERE [updated_by] IS NULL;');
        EXEC(N'ALTER TABLE [buyersystem].[{table}] ALTER COLUMN [updated_by] uniqueidentifier NOT NULL;');
    END
END");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (string table in Phase2ATables)
            {
                migrationBuilder.Sql($@"
IF OBJECT_ID(N'[buyersystem].[{table}]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'buyersystem.{table}', N'date_created') IS NOT NULL AND COL_LENGTH(N'buyersystem.{table}', N'created_on') IS NULL
        EXEC sp_rename N'buyersystem.{table}.date_created', N'created_on', N'COLUMN';

    IF COL_LENGTH(N'buyersystem.{table}', N'date_updated') IS NOT NULL AND COL_LENGTH(N'buyersystem.{table}', N'updated_on') IS NULL
        EXEC sp_rename N'buyersystem.{table}.date_updated', N'updated_on', N'COLUMN';
END");
            }
        }
    }
}
