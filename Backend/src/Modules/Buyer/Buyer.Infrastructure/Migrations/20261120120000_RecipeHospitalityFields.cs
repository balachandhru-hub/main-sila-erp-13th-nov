using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Additive hospitality fields on recipe / recipe_category (Food, Beverage, Minibar, Tobacco).</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261120120000_RecipeHospitalityFields")]
    public partial class RecipeHospitalityFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "product_class",
                schema: "buyersystem",
                table: "recipe",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipe_format",
                schema: "buyersystem",
                table: "recipe",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subcategory",
                schema: "buyersystem",
                table: "recipe",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "trim_loss_percent",
                schema: "buyersystem",
                table: "recipe",
                type: "decimal(9,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cooking_loss_percent",
                schema: "buyersystem",
                table: "recipe",
                type: "decimal(9,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "waste_factor_percent",
                schema: "buyersystem",
                table: "recipe",
                type: "decimal(9,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prep_time_minutes",
                schema: "buyersystem",
                table: "recipe",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cook_time_minutes",
                schema: "buyersystem",
                table: "recipe",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preparation_steps",
                schema: "buyersystem",
                table: "recipe",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_recipe_buyer_id_product_class",
                schema: "buyersystem",
                table: "recipe",
                columns: new[] { "buyer_id", "product_class" });

            migrationBuilder.AddColumn<string>(
                name: "product_class",
                schema: "buyersystem",
                table: "recipe_category",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_recipe_buyer_id_product_class",
                schema: "buyersystem",
                table: "recipe");

            migrationBuilder.DropColumn(name: "product_class", schema: "buyersystem", table: "recipe");
            migrationBuilder.DropColumn(name: "recipe_format", schema: "buyersystem", table: "recipe");
            migrationBuilder.DropColumn(name: "subcategory", schema: "buyersystem", table: "recipe");
            migrationBuilder.DropColumn(name: "trim_loss_percent", schema: "buyersystem", table: "recipe");
            migrationBuilder.DropColumn(name: "cooking_loss_percent", schema: "buyersystem", table: "recipe");
            migrationBuilder.DropColumn(name: "waste_factor_percent", schema: "buyersystem", table: "recipe");
            migrationBuilder.DropColumn(name: "prep_time_minutes", schema: "buyersystem", table: "recipe");
            migrationBuilder.DropColumn(name: "cook_time_minutes", schema: "buyersystem", table: "recipe");
            migrationBuilder.DropColumn(name: "preparation_steps", schema: "buyersystem", table: "recipe");
            migrationBuilder.DropColumn(name: "product_class", schema: "buyersystem", table: "recipe_category");
        }
    }
}
