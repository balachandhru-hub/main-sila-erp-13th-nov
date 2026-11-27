using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 4B Actual Consumption, Wastage, Returns, Reversals, Yield/Portion Variance, Production Settlement, Event Closure, ERP Reconciliation &amp; Historical Learning Foundation.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261129160000_SilaCulinaryPhase4BReconciliation")]
    public partial class SilaCulinaryPhase4BReconciliationMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_culinary_reconciliation_config",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    consumption_method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    backflush_requires_approval = table.Column<bool>(type: "bit", nullable: false),
                    inventory_valuation_method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    material_reconciliation_tolerance_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    waste_approval_threshold_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    yield_variance_threshold_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    portion_variance_threshold_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    food_cost_variance_threshold_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    labor_allocation_method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    overhead_allocation_method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    central_kitchen_allocation_method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    allow_provisional_settlement = table.Column<bool>(type: "bit", nullable: false),
                    financial_period_locked_before_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    closed_period_adjustment_policy = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    erp_posting_policy = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    inventory_system_of_record = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    max_leftover_holding_hours = table.Column<int>(type: "int", nullable: false),
                    require_disposal_photo_above_threshold = table.Column<bool>(type: "bit", nullable: false),
                    default_currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    config_version = table.Column<int>(type: "int", nullable: false),
                    effective_from_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    approval_matrix_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    config_history_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_reconciliation_config", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_reconciliation_config_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_consumption",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    sub_recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    consumption_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    inventory_batch_number = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    planned_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    issued_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    actual_consumed_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    consumption_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    consumption_method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    includes_waste = table.Column<bool>(type: "bit", nullable: false),
                    is_substitution = table.Column<bool>(type: "bit", nullable: false),
                    substituted_from_material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    capture_mode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    is_reversal = table.Column<bool>(type: "bit", nullable: false),
                    reversed_consumption_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    deviation_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    supporting_evidence = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    approval_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source_channel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recorded_by_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_production_consumption", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_production_consumption_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_additional_material_request",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    request_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    requested_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    approved_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    available_stock_snapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    estimated_cost_impact = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    reason_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    reason_notes = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    alternative_material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    alternative_store_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    emergency_authorized = table.Column<bool>(type: "bit", nullable: false),
                    approval_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    inventory_issued = table.Column<bool>(type: "bit", nullable: false),
                    inventory_issue_reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requested_by_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_additional_material_request", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_additional_material_request_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_material_return",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    original_material_issue_ref_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    returning_kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    receiving_store_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    return_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    inventory_batch_number = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    returned_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    return_classification = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ingredient_condition = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    expiry_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    return_reason = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    quality_inspection_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    routed_stock_bucket = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    returned_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    received_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    returned_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_material_return", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_material_return_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_material_issue_reversal",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    original_material_issue_ref_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    reversal_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    original_issue_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    inventory_batch_number = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    reversal_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    reversal_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    approval_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    erp_reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    reversed_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reversed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_material_issue_reversal", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_material_issue_reversal_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_waste_event",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    waste_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    item_domain = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    is_normal_recipe_yield_loss = table.Column<bool>(type: "bit", nullable: false),
                    already_deducted_from_inventory = table.Column<bool>(type: "bit", nullable: false),
                    item_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    item_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    inventory_batch_number = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    waste_category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    waste_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    estimated_waste_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    waste_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    disposal_method = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    photo_evidence_ref = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    approval_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    second_deduction_prevented = table.Column<bool>(type: "bit", nullable: false),
                    source_channel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recorded_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_waste_event", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_waste_event_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_food_leftover",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    leftover_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    source_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    finished_product_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    original_production_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    served_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    remaining_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    holding_conditions = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    production_timestamp_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    approved_use_by_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    storage_conditions = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    allergen_info = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    quality_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    food_safety_approved = table.Column<bool>(type: "bit", nullable: false),
                    disposition = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    disposition_history_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_food_leftover", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_food_leftover_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_waste_disposal",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    waste_event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    disposal_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    item_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    disposal_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    disposal_reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    disposal_method = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    disposal_location = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    disposed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    responsible_employee = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    supervisor_approval_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    supporting_evidence_ref = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    inventory_adjustment_ref = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    financial_waste_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_waste_disposal", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_waste_disposal_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_output_confirmation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    parent_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    confirmation_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    output_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    planned_output_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    actual_gross_output_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    accepted_usable_output_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    rejected_output_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    rework_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    finished_product_waste_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    output_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    actual_portions_produced = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    average_portion_weight_grams = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    quality_approval_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    responsible_chef = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    financially_settled = table.Column<bool>(type: "bit", nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_production_output_confirmation", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_production_output_confirmation_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_portion_sample",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    standard_portion_weight_grams = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    sampled_weights_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sample_count = table.Column<int>(type: "int", nullable: false),
                    average_actual_weight_grams = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    portion_weight_variance_percent = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    planned_portions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_portions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_usable_output_grams = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    estimated_portion_cost_variance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    sampling_basis_note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    sampled_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_portion_sample", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_portion_sample_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_reconciliation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    reconciliation_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    reconciliation_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    operational_settlement_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    financial_settlement_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    settlement_blocked = table.Column<bool>(type: "bit", nullable: false),
                    planned_production_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_direct_material_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_direct_labor_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_equipment_utility_packaging_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_fixed_overhead = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    fully_loaded_production_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    cost_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    cost_methodology = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    settlement_version = table.Column<int>(type: "int", nullable: false),
                    cost_variance_bridge_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    validation_checklist_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    finished_product_balance_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    reconciled_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    settled_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_production_reconciliation", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_production_reconciliation_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_production_reconciliation_line",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reconciliation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    inventory_batch_number = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    planned_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    reserved_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    picked_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    original_issued_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    additional_issued_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    reversed_issue_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    net_issued_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    net_inbound_transfer_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    net_outbound_transfer_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    usable_consumption_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    separate_waste_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    returned_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    remaining_wip_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    accounted_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    unexplained_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    tolerance_percent = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    line_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    settlement_blocked = table.Column<bool>(type: "bit", nullable: false),
                    planning_volume_variance_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    usage_variance_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    usage_cost_variance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    price_cost_variance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    source_transactions_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_production_reconciliation_line", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_labor_allocation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_task_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    employee_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    employee_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    staff_classification = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    actual_hours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    applicable_hourly_rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    overtime_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_labor_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    allocation_method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    is_estimated = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_labor_allocation", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_labor_allocation_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_central_kitchen_allocation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    receiving_outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    receiving_outlet_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    distributed_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    remaining_approved_stock_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    total_batch_production_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_production_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    internal_transfer_valuation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    treated_as_external_revenue = table.Column<bool>(type: "bit", nullable: false),
                    receiving_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    allocation_method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_central_kitchen_allocation", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_central_kitchen_allocation_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_buffet_reconciliation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    meal_period = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    service_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expected_covers = table.Column<int>(type: "int", nullable: false),
                    actual_covers = table.Column<int>(type: "int", nullable: false),
                    planned_buffet_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    initial_production_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    replenishment_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    total_production_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    total_dispatched_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    estimated_served_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    returned_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    leftover_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    waste_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    actual_ingredient_consumption_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    planned_production_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_production_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    waste_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_buffet_reconciliation", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_buffet_reconciliation_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_event_closure",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    closure_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    event_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    expected_guest_count = table.Column<int>(type: "int", nullable: false),
                    confirmed_guest_count = table.Column<int>(type: "int", nullable: false),
                    actual_guest_count = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    operational_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    financial_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    settlement_version = table.Column<int>(type: "int", nullable: false),
                    net_revenue_actual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    direct_material_cost_actual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    direct_labor_cost_actual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    external_expense_actual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_variable_cost_actual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    contribution_margin_actual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_overhead_actual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    fully_loaded_profit_actual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    pending_accrued_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    authoritative_revenue_source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    four_perspective_pnl_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    profitability_bridge_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pending_costs_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    operational_checklist_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    financial_checklist_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    closure_history_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    reopening_adjustments_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_event_closure", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_event_closure_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_erp_reconciliation_ref",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    sila_transaction_ref = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    erp_system = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    erp_document_ref = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    integration_message_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    posting_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    posted_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    error_message = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    reconciliation_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_erp_reconciliation_ref", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_erp_reconciliation_ref_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_historical_learning_snapshot",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    snapshot_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    dataset_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    data_maturity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    validated_for_training = table.Column<bool>(type: "bit", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    season = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    day_of_week_name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    meal_period = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    expected_guest_count = table.Column<int>(type: "int", nullable: false),
                    confirmed_guest_count = table.Column<int>(type: "int", nullable: false),
                    actual_guest_count = table.Column<int>(type: "int", nullable: false),
                    planned_food_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_food_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    planned_labor_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_labor_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_revenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_contribution_margin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_fully_loaded_profit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_waste_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    total_waste_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_yield_percent = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    portion_variance_percent = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    snapshot_payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    captured_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_historical_learning_snapshot", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_historical_learning_snapshot_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_reconciliation_audit",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    audit_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    summary = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    executed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_reconciliation_audit", x => x.id);
                    table.ForeignKey(name: "fk_sila_culinary_reconciliation_audit_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_consumption_buyer_id_consumption_number", schema: "buyersystem", table: "sila_culinary_production_consumption", columns: new[] { "buyer_id", "consumption_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_consumption_buyer_id_idempotency_key", schema: "buyersystem", table: "sila_culinary_production_consumption", columns: new[] { "buyer_id", "idempotency_key" }, unique: true, filter: "[idempotency_key] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_additional_material_request_buyer_id_request_number", schema: "buyersystem", table: "sila_culinary_additional_material_request", columns: new[] { "buyer_id", "request_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_material_return_buyer_id_return_number", schema: "buyersystem", table: "sila_culinary_material_return", columns: new[] { "buyer_id", "return_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_material_issue_reversal_buyer_id_reversal_number", schema: "buyersystem", table: "sila_culinary_material_issue_reversal", columns: new[] { "buyer_id", "reversal_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_waste_event_buyer_id_waste_code", schema: "buyersystem", table: "sila_culinary_waste_event", columns: new[] { "buyer_id", "waste_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_food_leftover_buyer_id_leftover_number", schema: "buyersystem", table: "sila_culinary_food_leftover", columns: new[] { "buyer_id", "leftover_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_waste_disposal_buyer_id_disposal_number", schema: "buyersystem", table: "sila_culinary_waste_disposal", columns: new[] { "buyer_id", "disposal_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_output_confirmation_buyer_id_confirmation_number", schema: "buyersystem", table: "sila_culinary_production_output_confirmation", columns: new[] { "buyer_id", "confirmation_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_production_reconciliation_buyer_id_reconciliation_number", schema: "buyersystem", table: "sila_culinary_production_reconciliation", columns: new[] { "buyer_id", "reconciliation_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_event_closure_buyer_id_closure_number", schema: "buyersystem", table: "sila_culinary_event_closure", columns: new[] { "buyer_id", "closure_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_erp_reconciliation_ref_buyer_id_idempotency_key", schema: "buyersystem", table: "sila_culinary_erp_reconciliation_ref", columns: new[] { "buyer_id", "idempotency_key" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_historical_learning_snapshot_buyer_id_snapshot_number", schema: "buyersystem", table: "sila_culinary_historical_learning_snapshot", columns: new[] { "buyer_id", "snapshot_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_reconciliation_audit_buyer_id_executed_at_utc", schema: "buyersystem", table: "sila_culinary_reconciliation_audit", columns: new[] { "buyer_id", "executed_at_utc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sila_culinary_reconciliation_audit", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_historical_learning_snapshot", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_erp_reconciliation_ref", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_event_closure", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_buffet_reconciliation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_central_kitchen_allocation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_labor_allocation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_reconciliation_line", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_reconciliation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_portion_sample", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_output_confirmation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_waste_disposal", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_food_leftover", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_waste_event", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_material_issue_reversal", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_material_return", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_additional_material_request", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_production_consumption", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_reconciliation_config", schema: "buyersystem");
        }
    }
}
