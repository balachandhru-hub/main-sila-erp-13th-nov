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
            migrationBuilder.AddColumn<string>(
                name: "transaction_type",
                schema: "buyersystem",
                table: "pos_sales_transaction",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SALE");

            migrationBuilder.AddColumn<string>(
                name: "original_transaction_id",
                schema: "buyersystem",
                table: "pos_sales_transaction",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "original_line_id",
                schema: "buyersystem",
                table: "pos_sales_transaction",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_sales_transaction_buyer_id_original_transaction_id",
                schema: "buyersystem",
                table: "pos_sales_transaction",
                columns: new[] { "buyer_id", "original_transaction_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pos_sales_transaction_buyer_id_original_transaction_id",
                schema: "buyersystem",
                table: "pos_sales_transaction");

            migrationBuilder.DropColumn(
                name: "original_line_id",
                schema: "buyersystem",
                table: "pos_sales_transaction");

            migrationBuilder.DropColumn(
                name: "original_transaction_id",
                schema: "buyersystem",
                table: "pos_sales_transaction");

            migrationBuilder.DropColumn(
                name: "transaction_type",
                schema: "buyersystem",
                table: "pos_sales_transaction");
        }
    }
}
