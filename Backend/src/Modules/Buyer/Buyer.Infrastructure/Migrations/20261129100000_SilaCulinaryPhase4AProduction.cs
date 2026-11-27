using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 4A Kitchen Production Planning, Batch Scheduling, Execution, KDS, Quality Checkpoints, Central Kitchen Dispatch, and Audit.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261129100000_SilaCulinaryPhase4AProduction")]
    public partial class SilaCulinaryPhase4AProductionMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_culinary_kitchen",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    kitchen_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    kitchen_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    kitchen_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    associated_store_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    responsible_executive_chef = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    kitchen_supervisor = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    daily_portion_capacity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    outlet_associations_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    supported_meal_periods_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    supported_categories_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    shift_config_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    effective_from_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    effective_to_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_kitchen", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_kitchen_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_workstation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    workstation_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    workstation_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    workstation_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    concurrent_capacity = table.Column<int>(type: "int", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_workstation", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_workstation_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_equipment",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    workstation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    equipment_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    equipment_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    equipment_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    batch_capacity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    capacity_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    setup_minutes = table.Column<int>(type: "int", nullable: false),
                    cleaning_minutes = table.Column<int>(type: "int", nullable: false),
                    available_from_minute = table.Column<int>(type: "int", nullable: false),
                    available_until_minute = table.Column<int>(type: "int", nullable: false),
                    under_maintenance = table.Column<bool>(type: "bit", nullable: false),
                    temperature_capability = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    allergen_segregated = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_equipment", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_equipment_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_staff_assignment",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    staff_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    culinary_role = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    workstation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    shift_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    skill_classification = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    supervisor_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    authorized_activities_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    available_from_minute = table.Column<int>(type: "int", nullable: false),
                    available_until_minute = table.Column<int>(type: "int", nullable: false),
                    is_temporary = table.Column<bool>(type: "bit", nullable: false),
                    is_cross_kitchen = table.Column<bool>(type: "bit", nullable: false),
                    is_available = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_staff_assignment", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_staff_assignment_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_demand",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_document_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    demand_source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    demand_nature = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    menu_item_name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    meal_period = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    production_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    required_completion_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    forecast_portions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    adjusted_portions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    conversion_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    production_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_production_demand", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_production_demand_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_plan",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plan_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    production_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    planning_period = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    meal_period = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    responsible_chef = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    source_demand_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    approved_by_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    total_estimated_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_production_plan", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_production_plan_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_plan_line",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    menu_item_name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    recipe_version = table.Column<int>(type: "int", nullable: false),
                    required_portions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    planned_production_qty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    production_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    workstation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    planned_start_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    required_completion_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    ingredient_readiness = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    equipment_readiness = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    staff_readiness = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    estimated_food_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    line_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_plan_line", x => x.id); });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_plan_version",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    change_summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_plan_version", x => x.id); });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_order",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    production_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_version = table.Column<int>(type: "int", nullable: false),
                    recipe_name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    order_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    planned_output_qty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_output_qty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    output_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    scheduled_start_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    required_completion_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    responsible_chef = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    estimated_production_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    approval_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    execution_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    immutable_recipe_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_production_order", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_production_order_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_order_ingredient",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    gross_required_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    net_usable_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    yield_adjusted_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    inventory_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    available_eligible_stock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    incoming_before_production_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    existing_commitments_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    shortage_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    readiness_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    traceability_source_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_order_ingredient", x => x.id); });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_batch",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    batch_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    planned_batch_qty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_output_qty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    production_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    workstation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    equipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    assigned_staff_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    assigned_chef_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    planned_start_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    planned_finish_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    actual_start_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    actual_finish_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    batch_priority = table.Column<int>(type: "int", nullable: false),
                    quality_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    batch_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    parent_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    use_by_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    allergen_summary = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    qr_payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    recipe_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_production_batch", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_production_batch_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_task",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence_order = table.Column<int>(type: "int", nullable: false),
                    depends_on_task_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    task_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    instructions = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    workstation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    equipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    assigned_staff_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    planned_duration_minutes = table.Column<int>(type: "int", nullable: false),
                    actual_duration_minutes = table.Column<int>(type: "int", nullable: true),
                    actual_start_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    actual_completion_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    exception_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_task", x => x.id); });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_picking_line",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    stock_reservation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    required_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    reserved_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    picked_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    inventory_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    store_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    inventory_batch_number = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    expiry_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    picking_strategy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_picking_line", x => x.id); });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_material_issue_ref",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    goods_issue_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    issue_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    issued_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    issued_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    from_store_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    to_kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    is_reversed = table.Column<bool>(type: "bit", nullable: false),
                    issued_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_material_issue_ref", x => x.id); });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_quality_checkpoint",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    checkpoint_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    checkpoint_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    stage = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    expected_criteria = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    measurement_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_mandatory = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_quality_checkpoint", x => x.id); });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_quality_result",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    checkpoint_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    checkpoint_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    expected_value = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    actual_value = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    measurement_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    passed = table.Column<bool>(type: "bit", nullable: false),
                    recorded_by_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    recorded_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    comments = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    corrective_action = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    supervisor_approved = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_quality_result", x => x.id); });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_exception",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exception_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    exception_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    reported_by_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    assigned_owner = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    recommended_action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    resolution_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    resolution_note = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_exception", x => x.id); });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_dispatch",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    dispatch_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    central_kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    destination_outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    destination_outlet_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    requested_qty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_qty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    dispatched_qty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    received_qty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    required_delivery_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    internal_transfer_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_dispatch", x => x.id); });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_audit",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    action_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    summary = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    executed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table => { table.PrimaryKey("pk_sila_culinary_production_audit", x => x.id); });

            migrationBuilder.CreateIndex(name: "ix_sila_culinary_kitchen_buyer_id_kitchen_code", schema: "buyersystem", table: "sila_culinary_kitchen", columns: new[] { "buyer_id", "kitchen_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_workstation_buyer_id_kitchen_id_workstation_code", schema: "buyersystem", table: "sila_culinary_workstation", columns: new[] { "buyer_id", "kitchen_id", "workstation_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_equipment_buyer_id_kitchen_id_equipment_code", schema: "buyersystem", table: "sila_culinary_equipment", columns: new[] { "buyer_id", "kitchen_id", "equipment_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_staff_assignment_buyer_id_kitchen_id_user_id", schema: "buyersystem", table: "sila_culinary_staff_assignment", columns: new[] { "buyer_id", "kitchen_id", "user_id" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_demand_buyer_id_source_document_key", schema: "buyersystem", table: "sila_culinary_production_demand", columns: new[] { "buyer_id", "source_document_key" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_plan_buyer_id_plan_code", schema: "buyersystem", table: "sila_culinary_production_plan", columns: new[] { "buyer_id", "plan_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_order_buyer_id_order_number", schema: "buyersystem", table: "sila_culinary_production_order", columns: new[] { "buyer_id", "order_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_batch_buyer_id_batch_number", schema: "buyersystem", table: "sila_culinary_production_batch", columns: new[] { "buyer_id", "batch_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_material_issue_ref_buyer_id_idempotency_key", schema: "buyersystem", table: "sila_culinary_production_material_issue_ref", columns: new[] { "buyer_id", "idempotency_key" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_exception_buyer_id_exception_code", schema: "buyersystem", table: "sila_culinary_production_exception", columns: new[] { "buyer_id", "exception_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_dispatch_buyer_id_dispatch_number", schema: "buyersystem", table: "sila_culinary_production_dispatch", columns: new[] { "buyer_id", "dispatch_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_audit_buyer_id_executed_at_utc", schema: "buyersystem", table: "sila_culinary_production_audit", columns: new[] { "buyer_id", "executed_at_utc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sila_culinary_production_audit", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_dispatch", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_exception", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_quality_result", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_quality_checkpoint", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_material_issue_ref", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_picking_line", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_task", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_batch", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_order_ingredient", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_order", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_plan_version", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_plan_line", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_plan", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_demand", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_staff_assignment", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_equipment", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_workstation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_kitchen", schema: "buyersystem");
        }
    }
}
