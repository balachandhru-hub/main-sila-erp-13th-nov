using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 2C Culinary Intelligence: Procurement Plans, Lines, Atomic Stock Reservations, Allocations/Traceability, Exceptions, and Scenarios.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261128010000_SilaCulinaryPhase2CProcurement")]
    public partial class SilaCulinaryPhase2CProcurementMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_culinary_procurement_plan",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plan_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    mrp_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mrp_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    mrp_version = table.Column<int>(type: "int", nullable: false),
                    primary_event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    primary_event_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    primary_event_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    plant_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    is_simulation = table.Column<bool>(type: "bit", nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    event_date_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    preparation_deadline_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    planned_guests = table.Column<int>(type: "int", nullable: false),
                    guaranteed_guests = table.Column<int>(type: "int", nullable: false),
                    responsible_chef = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    venue_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    coverage_priority_json = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    event_references_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supply_demand_ledger_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    consolidated_purchases_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    readiness_summary_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phase3a_handover_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    required_materials_count = table.Column<int>(type: "int", nullable: false),
                    fully_reserved_count = table.Column<int>(type: "int", nullable: false),
                    incoming_po_covered_count = table.Column<int>(type: "int", nullable: false),
                    transfer_covered_count = table.Column<int>(type: "int", nullable: false),
                    purchase_required_count = table.Column<int>(type: "int", nullable: false),
                    critical_shortage_count = table.Column<int>(type: "int", nullable: false),
                    delayed_delivery_count = table.Column<int>(type: "int", nullable: false),
                    pending_approval_count = table.Column<int>(type: "int", nullable: false),
                    estimated_requirement_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    estimated_new_purchase_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    committed_procurement_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    physical_readiness_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    projected_readiness_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    submitted_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    submitted_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approval_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    calculated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_procurement_plan", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_procurement_plan_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_procurement_line",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    procurement_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    event_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    mrp_line_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    material_description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    material_category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    material_image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    base_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    purchasing_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    destination_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    destination_kitchen_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    destination_storage_location = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    required_by_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    gross_requirement_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    local_on_hand_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    competing_reservations_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    safety_stock_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    eligible_local_stock_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    active_event_reservation_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    proposed_reservation_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    prepared_stock_coverage_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    incoming_po_allocated_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    incoming_transfer_allocated_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    proposed_ito_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    confirmed_coverage_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    remaining_confirmed_gap_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    net_uncovered_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    recommended_purchase_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    expected_excess_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    pack_size_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    moq_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    number_of_packs = table.Column<int>(type: "int", nullable: true),
                    recommended_supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recommended_supplier_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    recommended_supplier_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    contract_reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    supplier_unit_price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    estimated_purchase_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    expected_delivery_date_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    latest_order_release_date_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_delivery_feasible = table.Column<bool>(type: "bit", nullable: false),
                    coverage_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    readiness_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    weekly_bucket_linked = table.Column<bool>(type: "bit", nullable: false),
                    weekly_bucket_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_details_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supplier_comparisons_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fefo_batches_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_procurement_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_procurement_line_sila_culinary_procurement_plan_procurement_plan_id",
                        column: x => x.procurement_plan_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_procurement_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_stock_reservation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reservation_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    procurement_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    procurement_line_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_revision = table.Column<int>(type: "int", nullable: false),
                    session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    mrp_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mrp_version = table.Column<int>(type: "int", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    material_description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    plant_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    storage_location_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    batch_number = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    batch_expiry_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reserved_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    fulfilled_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    released_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    base_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    required_by_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    is_prepared_stock = table.Column<bool>(type: "bit", nullable: false),
                    safety_stock_override_used = table.Column<bool>(type: "bit", nullable: false),
                    safety_override_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    priority_rank = table.Column<int>(type: "int", nullable: false),
                    approval_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_stock_reservation", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_stock_reservation_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_procurement_allocation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    procurement_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    procurement_line_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_revision = table.Column<int>(type: "int", nullable: false),
                    mrp_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mrp_version = table.Column<int>(type: "int", nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    material_description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    allocation_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_location_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    destination_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    destination_location_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    purchase_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    purchase_order_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    po_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    po_item_number = table.Column<int>(type: "int", nullable: true),
                    internal_transfer_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ito_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    goods_receipt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    grn_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    sap_material_document = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    reversal_material_document = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    supplier_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    contract_reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    account_assignment_category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    cost_center = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    gl_account = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    internal_order = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    wbs_element = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    profit_center = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    allocated_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    received_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    reversed_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    remaining_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    estimated_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    expected_date_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_procurement_allocation", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_procurement_allocation_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_procurement_exception",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    procurement_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    procurement_line_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    material_description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    exception_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    quantity_impact = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    required_date_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    responsible_role = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    recommended_action = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    resolution_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    resolution_note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    resolved_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_procurement_exception", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_procurement_exception_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_procurement_scenario",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    procurement_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    scenario_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    parameters_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    results_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    estimated_purchase_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    projected_readiness_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_procurement_scenario", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_procurement_scenario_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "ix_sila_culinary_procurement_plan_buyer_id_plan_code", schema: "buyersystem", table: "sila_culinary_procurement_plan", columns: new[] { "buyer_id", "plan_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_procurement_plan_buyer_id_mrp_run_id_status", schema: "buyersystem", table: "sila_culinary_procurement_plan", columns: new[] { "buyer_id", "mrp_run_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_procurement_plan_buyer_id_idempotency_key", schema: "buyersystem", table: "sila_culinary_procurement_plan", columns: new[] { "buyer_id", "idempotency_key" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_procurement_line_buyer_id_procurement_plan_id_material_code", schema: "buyersystem", table: "sila_culinary_procurement_line", columns: new[] { "buyer_id", "procurement_plan_id", "material_code" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_stock_reservation_buyer_id_reservation_code", schema: "buyersystem", table: "sila_culinary_stock_reservation", columns: new[] { "buyer_id", "reservation_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_stock_reservation_buyer_id_location_id_material_id_status", schema: "buyersystem", table: "sila_culinary_stock_reservation", columns: new[] { "buyer_id", "location_id", "material_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_stock_reservation_buyer_id_event_id_status", schema: "buyersystem", table: "sila_culinary_stock_reservation", columns: new[] { "buyer_id", "event_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_procurement_allocation_buyer_id_procurement_plan_id_allocation_type", schema: "buyersystem", table: "sila_culinary_procurement_allocation", columns: new[] { "buyer_id", "procurement_plan_id", "allocation_type" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_procurement_allocation_buyer_id_purchase_order_item_id_status", schema: "buyersystem", table: "sila_culinary_procurement_allocation", columns: new[] { "buyer_id", "purchase_order_item_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_procurement_allocation_buyer_id_internal_transfer_order_id_status", schema: "buyersystem", table: "sila_culinary_procurement_allocation", columns: new[] { "buyer_id", "internal_transfer_order_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_procurement_allocation_buyer_id_idempotency_key", schema: "buyersystem", table: "sila_culinary_procurement_allocation", columns: new[] { "buyer_id", "idempotency_key" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_procurement_exception_buyer_id_procurement_plan_id_resolution_status", schema: "buyersystem", table: "sila_culinary_procurement_exception", columns: new[] { "buyer_id", "procurement_plan_id", "resolution_status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_procurement_scenario_buyer_id_procurement_plan_id", schema: "buyersystem", table: "sila_culinary_procurement_scenario", columns: new[] { "buyer_id", "procurement_plan_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sila_culinary_procurement_scenario", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_procurement_exception", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_procurement_allocation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_stock_reservation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_procurement_line", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_procurement_plan", schema: "buyersystem");
        }
    }
}
