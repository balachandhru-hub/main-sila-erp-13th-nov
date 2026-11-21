using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Goods Issue cost center mappings: Property-Plant and Property-Plant-StorageLocation.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261120140000_GoodsIssueCostCenter")]
    public partial class GoodsIssueCostCenter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "goods_issue_cost_center",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plant_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    storage_location_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    cost_center = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goods_issue_cost_center", x => x.id);
                    table.ForeignKey(
                        name: "fk_goods_issue_cost_center_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_goods_issue_cost_center_buyer_property_property_id",
                        column: x => x.property_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_goods_issue_cost_center_is_active",
                schema: "buyersystem",
                table: "goods_issue_cost_center",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_goods_issue_cost_center_property_id",
                schema: "buyersystem",
                table: "goods_issue_cost_center",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_goods_issue_cost_center_buyer_id_property_id_scope_storage_location_code",
                schema: "buyersystem",
                table: "goods_issue_cost_center",
                columns: new[] { "buyer_id", "property_id", "scope", "storage_location_code" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "goods_issue_cost_center",
                schema: "buyersystem");
        }
    }
}
