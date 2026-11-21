using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Additive columns for controlled POS RETURN/VOID (Z01) without destructive changes.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261119203000_PosSalesReversalFields")]
    public partial class PosSalesReversalFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: columns may already exist from earlier manual/hotfix applies.
            migrationBuilder.Sql("""
IF COL_LENGTH('buyersystem.pos_sales_transaction', 'transaction_type') IS NULL
BEGIN
    ALTER TABLE [buyersystem].[pos_sales_transaction]
    ADD [transaction_type] nvarchar(20) NOT NULL CONSTRAINT DF_pos_sales_transaction_transaction_type DEFAULT N'SALE';
END

IF COL_LENGTH('buyersystem.pos_sales_transaction', 'original_transaction_id') IS NULL
BEGIN
    ALTER TABLE [buyersystem].[pos_sales_transaction]
    ADD [original_transaction_id] nvarchar(100) NULL;
END

IF COL_LENGTH('buyersystem.pos_sales_transaction', 'original_line_id') IS NULL
BEGIN
    ALTER TABLE [buyersystem].[pos_sales_transaction]
    ADD [original_line_id] int NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_pos_sales_transaction_buyer_id_original_transaction_id'
      AND object_id = OBJECT_ID(N'buyersystem.pos_sales_transaction'))
BEGIN
    CREATE INDEX [IX_pos_sales_transaction_buyer_id_original_transaction_id]
    ON [buyersystem].[pos_sales_transaction] ([buyer_id], [original_transaction_id]);
END
""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_pos_sales_transaction_buyer_id_original_transaction_id'
      AND object_id = OBJECT_ID(N'buyersystem.pos_sales_transaction'))
    DROP INDEX [IX_pos_sales_transaction_buyer_id_original_transaction_id]
    ON [buyersystem].[pos_sales_transaction];

IF COL_LENGTH('buyersystem.pos_sales_transaction', 'original_line_id') IS NOT NULL
    ALTER TABLE [buyersystem].[pos_sales_transaction] DROP COLUMN [original_line_id];

IF COL_LENGTH('buyersystem.pos_sales_transaction', 'original_transaction_id') IS NOT NULL
    ALTER TABLE [buyersystem].[pos_sales_transaction] DROP COLUMN [original_transaction_id];

IF COL_LENGTH('buyersystem.pos_sales_transaction', 'transaction_type') IS NOT NULL
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'buyersystem.pos_sales_transaction')
      AND c.name = N'transaction_type';
    IF @df IS NOT NULL
        EXEC(N'ALTER TABLE [buyersystem].[pos_sales_transaction] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [buyersystem].[pos_sales_transaction] DROP COLUMN [transaction_type];
END
""");
        }
    }
}
