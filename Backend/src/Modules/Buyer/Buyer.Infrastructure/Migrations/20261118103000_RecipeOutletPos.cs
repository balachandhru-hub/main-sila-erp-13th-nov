using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Outlet-specific POS code / POS item on recipe_outlet_price (Menu and outlet assignment).
    /// Additive only — existing rows keep null until backfilled or re-saved.
    /// </remarks>
    public partial class RecipeOutletPos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pos_code",
                schema: "buyersystem",
                table: "recipe_outlet_price",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pos_item",
                schema: "buyersystem",
                table: "recipe_outlet_price",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pos_code",
                schema: "buyersystem",
                table: "recipe_outlet_price");

            migrationBuilder.DropColumn(
                name: "pos_item",
                schema: "buyersystem",
                table: "recipe_outlet_price");
        }
    }
}
