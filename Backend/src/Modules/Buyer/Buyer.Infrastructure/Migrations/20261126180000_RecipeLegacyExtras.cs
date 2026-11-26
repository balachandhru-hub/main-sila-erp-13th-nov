using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Additive extras_json on recipe_legacy_recipe for staged rows of the recipe workbook format.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261126180000_RecipeLegacyExtras")]
    public partial class RecipeLegacyExtras : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "extras_json",
                schema: "buyersystem",
                table: "recipe_legacy_recipe",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "extras_json",
                schema: "buyersystem",
                table: "recipe_legacy_recipe");
        }
    }
}
