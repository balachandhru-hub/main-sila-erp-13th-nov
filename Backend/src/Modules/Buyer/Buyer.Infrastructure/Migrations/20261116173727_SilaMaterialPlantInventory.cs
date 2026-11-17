using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Minimal extension: material plant / storage-location assignments, ERP stock snapshots, integration exceptions.
    /// Unrelated model drift from scaffolding was stripped. Do not apply to live DBs without authorization.
    /// Snapshots never overwrite InventoryBalance / InventoryLedger.
    /// </remarks>
    public partial class SilaMaterialPlantInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "material_erp_stock_snapshot",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plant_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    storage_location_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    stock_category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    batch = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    special_stock = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    base_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    snapshot_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    source_system = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    inventory_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_erp_stock_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_erp_stock_snapshot_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_integration_exception",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    exception_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    plant_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    storage_location_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    resolved = table.Column<bool>(type: "bit", nullable: false),
                    resolved_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_integration_exception", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_integration_exception_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_plant_assignment",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plant_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    plant_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    purchasing_group = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    mrp_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    procurement_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    reorder_point = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    safety_stock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    min_lot_size = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    max_lot_size = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    planned_delivery_days = table.Column<int>(type: "int", nullable: true),
                    last_synced_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    source_system = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_plant_assignment", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_plant_assignment_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_storage_location_assignment",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plant_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    storage_location_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    inventory_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    assignment_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    last_synced_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    source_system = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_storage_location_assignment", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_storage_location_assignment_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_material_erp_stock_snapshot_buyer_id_material_id_plant_code_storage_location_code_stock_category_batch",
                schema: "buyersystem",
                table: "material_erp_stock_snapshot",
                columns: new[] { "buyer_id", "material_id", "plant_code", "storage_location_code", "stock_category", "batch" });

            migrationBuilder.CreateIndex(
                name: "ix_material_integration_exception_buyer_id_resolved_exception_type",
                schema: "buyersystem",
                table: "material_integration_exception",
                columns: new[] { "buyer_id", "resolved", "exception_type" });

            migrationBuilder.CreateIndex(
                name: "ix_material_plant_assignment_buyer_id_material_id_plant_code",
                schema: "buyersystem",
                table: "material_plant_assignment",
                columns: new[] { "buyer_id", "material_id", "plant_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_material_storage_location_assignment_buyer_id_material_id_plant_code_storage_location_code",
                schema: "buyersystem",
                table: "material_storage_location_assignment",
                columns: new[] { "buyer_id", "material_id", "plant_code", "storage_location_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "material_erp_stock_snapshot",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "material_integration_exception",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "material_plant_assignment",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "material_storage_location_assignment",
                schema: "buyersystem");
        }
    }
}
