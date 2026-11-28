using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase ME.5 additive schema for 12 independent feature flags and customer data migration audit tracking.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261204010000_MenuEngineeringPhase5Readiness")]
    public partial class MenuEngineeringPhase5Readiness : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "feature_flags_json",
                schema: "buyersystem",
                table: "menu_engineering_settings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "data_migration_audits_json",
                schema: "buyersystem",
                table: "menu_engineering_settings",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "feature_flags_json",
                schema: "buyersystem",
                table: "menu_engineering_settings");

            migrationBuilder.DropColumn(
                name: "data_migration_audits_json",
                schema: "buyersystem",
                table: "menu_engineering_settings");
        }
    }
}
