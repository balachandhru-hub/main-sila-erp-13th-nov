using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 2B Culinary Intelligence: Event MRP Runs, Consolidated Lines, Exceptions, Scenarios, and Immutable Snapshots.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261127233000_SilaCulinaryPhase2BMrp")]
    public partial class SilaCulinaryPhase2BMrpMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_culinary_mrp_run",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mrp_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    run_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    is_approved_baseline = table.Column<bool>(type: "bit", nullable: false),
                    is_simulation = table.Column<bool>(type: "bit", nullable: false),
                    engine_version = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    plant_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    planning_horizon_start_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    planning_horizon_end_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    calculated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    buffer_policy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    prepared_stock_mode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    event_references_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    event_revisions_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    selected_sessions_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    selected_kitchens_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    input_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    output_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    explosion_tree_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    time_phased_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    batch_summaries_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    readiness_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    total_events = table.Column<int>(type: "int", nullable: false),
                    total_recipes = table.Column<int>(type: "int", nullable: false),
                    total_unique_materials = table.Column<int>(type: "int", nullable: false),
                    sufficient_stock_count = table.Column<int>(type: "int", nullable: false),
                    shortage_count = table.Column<int>(type: "int", nullable: false),
                    unknown_stock_count = table.Column<int>(type: "int", nullable: false),
                    missing_mapping_count = table.Column<int>(type: "int", nullable: false),
                    blocking_exception_count = table.Column<int>(type: "int", nullable: false),
                    warning_exception_count = table.Column<int>(type: "int", nullable: false),
                    estimated_requirement_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    indicative_shortage_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    inventory_timestamp_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    next_required_by_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    parent_mrp_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    submitted_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    submitted_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approval_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    approval_decision_comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    released_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    released_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_mrp_run", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_mrp_run_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_mrp_line",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mrp_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    consolidation_key = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    material_description = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    material_category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_resolved_material = table.Column<bool>(type: "bit", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    plant_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    kitchen_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    storage_location_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    required_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    required_date_bucket = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    base_uom = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    edible_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    raw_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    gross_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    prepared_stock_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    indicative_new_production_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    eligible_stock_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    incoming_po_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    indicative_current_shortage_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    indicative_remaining_shortage_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    unit_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    cost_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    requirement_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    indicative_shortage_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    validation_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_blocking = table.Column<bool>(type: "bit", nullable: false),
                    inventory_timestamp_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    contributing_recipes_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contributing_events_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contributing_sessions_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sub_recipe_paths_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    exclusion_reasons_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_mrp_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_mrp_line_sila_culinary_mrp_run_mrp_run_id",
                        column: x => x.mrp_run_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_mrp_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_mrp_exception",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mrp_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exception_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    exception_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    material_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    calculation_impact = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    responsible_team = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    recommended_action = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    linked_workflow = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    resolution_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    resolution_note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    resolved_by_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    resolved_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cycle_path_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_mrp_exception", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_mrp_exception_sila_culinary_mrp_run_mrp_run_id",
                        column: x => x.mrp_run_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_mrp_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_mrp_scenario",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mrp_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    scenario_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    scenario_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    is_baseline = table.Column<bool>(type: "bit", nullable: false),
                    planned_guests = table.Column<int>(type: "int", nullable: false),
                    total_requirement_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    total_shortage_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    shortage_count = table.Column<int>(type: "int", nullable: false),
                    exception_count = table.Column<int>(type: "int", nullable: false),
                    parameters_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    result_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_mrp_scenario", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_mrp_scenario_sila_culinary_mrp_run_mrp_run_id",
                        column: x => x.mrp_run_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_mrp_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_mrp_snapshot",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mrp_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    status_at_snapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    is_approved_baseline = table.Column<bool>(type: "bit", nullable: false),
                    change_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    approval_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    input_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    output_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_mrp_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_mrp_snapshot_sila_culinary_mrp_run_mrp_run_id",
                        column: x => x.mrp_run_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_mrp_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "ix_sila_culinary_mrp_run_buyer_id_mrp_code", schema: "buyersystem", table: "sila_culinary_mrp_run", columns: new[] { "buyer_id", "mrp_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_mrp_run_buyer_id_status", schema: "buyersystem", table: "sila_culinary_mrp_run", columns: new[] { "buyer_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_mrp_run_buyer_id_property_id", schema: "buyersystem", table: "sila_culinary_mrp_run", columns: new[] { "buyer_id", "property_id" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_mrp_line_buyer_id_mrp_run_id_required_date_bucket", schema: "buyersystem", table: "sila_culinary_mrp_line", columns: new[] { "buyer_id", "mrp_run_id", "required_date_bucket" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_mrp_line_buyer_id_material_id", schema: "buyersystem", table: "sila_culinary_mrp_line", columns: new[] { "buyer_id", "material_id" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_mrp_exception_buyer_id_mrp_run_id_resolution_status", schema: "buyersystem", table: "sila_culinary_mrp_exception", columns: new[] { "buyer_id", "mrp_run_id", "resolution_status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_mrp_scenario_buyer_id_mrp_run_id", schema: "buyersystem", table: "sila_culinary_mrp_scenario", columns: new[] { "buyer_id", "mrp_run_id" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_mrp_snapshot_buyer_id_mrp_run_id_version_number", schema: "buyersystem", table: "sila_culinary_mrp_snapshot", columns: new[] { "buyer_id", "mrp_run_id", "version_number" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sila_culinary_mrp_snapshot", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_mrp_scenario", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_mrp_exception", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_mrp_line", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_mrp_run", schema: "buyersystem");
        }
    }
}
