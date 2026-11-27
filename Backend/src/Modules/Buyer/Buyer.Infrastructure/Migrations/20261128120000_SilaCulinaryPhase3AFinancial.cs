using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 3A Culinary Intelligence: Financial Config, Event Financial Plans, Revenue Lines, Expense/Manpower Lines, Exceptions, and Scenarios.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261128120000_SilaCulinaryPhase3AFinancial")]
    public partial class SilaCulinaryPhase3AFinancialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_culinary_financial_config",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reporting_currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    property_currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    financial_precision = table.Column<int>(type: "int", nullable: false),
                    quantity_precision = table.Column<int>(type: "int", nullable: false),
                    monetary_rounding_policy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    cost_source_priority_json = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    cost_valuation_policy = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    target_food_cost_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    target_beverage_cost_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    target_event_contribution_margin_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    material_cost_alert_threshold_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    budget_approval_threshold_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    allow_manual_cost_overrides = table.Column<bool>(type: "bit", nullable: false),
                    exclude_vat_from_revenue = table.Column<bool>(type: "bit", nullable: false),
                    default_vat_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    exchange_rate_source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    exchange_rates_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    overhead_rules_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fiscal_year_start_month = table.Column<int>(type: "int", nullable: false),
                    cost_snapshot_policy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_financial_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_financial_config_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_event_financial_plan",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plan_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    event_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    event_revision = table.Column<int>(type: "int", nullable: false),
                    menu_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    menu_revision = table.Column<int>(type: "int", nullable: true),
                    mrp_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    mrp_version = table.Column<int>(type: "int", nullable: true),
                    procurement_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    procurement_plan_version = table.Column<int>(type: "int", nullable: true),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    venue_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    customer_reference = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    event_date_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    expected_guests = table.Column<int>(type: "int", nullable: false),
                    guaranteed_guests = table.Column<int>(type: "int", nullable: false),
                    planned_guests = table.Column<int>(type: "int", nullable: false),
                    actual_guests = table.Column<int>(type: "int", nullable: true),
                    billable_guests = table.Column<int>(type: "int", nullable: false),
                    billable_policy = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    revenue_model = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    reporting_currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    is_simulation = table.Column<bool>(type: "bit", nullable: false),
                    is_demo = table.Column<bool>(type: "bit", nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    target_food_cost_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    target_beverage_cost_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    target_contribution_margin_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    original_budget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    revised_budget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    gross_revenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    net_revenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_food_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_beverage_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_manpower_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_equipment_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_decoration_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_external_service_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_other_direct_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_allocated_overhead = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_total_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    approved_procurement_commitment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    attributable_procurement_commitment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    expected_consumption_value_from_po = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    remaining_unallocated_inventory_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_verified_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_is_provisional = table.Column<bool>(type: "bit", nullable: false),
                    forecast_at_completion_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    direct_contribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    contribution_after_overhead = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    contribution_margin_percent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    food_cost_percent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    beverage_cost_percent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    break_even_guests = table.Column<int>(type: "int", nullable: true),
                    max_allowable_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    remaining_cost_headroom = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    cost_center = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    profit_center = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    internal_order = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    wbs_element = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    financial_owner = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    calculation_engine_version = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    snapshot_payload_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phase3b_handover_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phase4b_handover_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("pk_sila_culinary_event_financial_plan", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_event_financial_plan_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_event_revenue_line",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_financial_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    revenue_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    billing_basis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    gross_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    net_revenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    source_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_event_revenue_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_event_revenue_line_sila_culinary_event_financial_plan_event_financial_plan_id",
                        column: x => x.event_financial_plan_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_event_financial_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_event_expense_line",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_financial_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    expense_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    cost_behavior = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    step_threshold_guests = table.Column<int>(type: "int", nullable: true),
                    step_increment_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    supplier_or_employee_ref = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    employee_role = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    staff_count = table.Column<int>(type: "int", nullable: true),
                    planned_hours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    actual_hours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    hourly_rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    overtime_hours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    overtime_rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    fixed_shift_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    external_agency_charge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    unit_rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    budget_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    committed_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_verified_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    tax_treatment = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    cost_center = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    gl_account = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    internal_order = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    wbs_element = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    supporting_document_ref = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    expense_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    approval_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    source_transaction = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    source_cost_pool_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_event_expense_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_event_expense_line_sila_culinary_event_financial_plan_event_financial_plan_id",
                        column: x => x.event_financial_plan_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_event_financial_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_financial_exception",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_financial_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exception_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    material_or_recipe_ref = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    financial_impact = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    message = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    recommended_action = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    responsible_user = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
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
                    table.PrimaryKey("pk_sila_culinary_financial_exception", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_financial_exception_sila_culinary_event_financial_plan_event_financial_plan_id",
                        column: x => x.event_financial_plan_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_event_financial_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_financial_scenario",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_financial_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    scenario_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    simulated_guests = table.Column<int>(type: "int", nullable: false),
                    simulated_revenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    simulated_total_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    simulated_contribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    simulated_margin_percent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    simulated_break_even_guests = table.Column<int>(type: "int", nullable: true),
                    parameters_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    results_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    promoted_to_revision = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_financial_scenario", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_financial_scenario_sila_culinary_event_financial_plan_event_financial_plan_id",
                        column: x => x.event_financial_plan_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_event_financial_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "ix_sila_culinary_financial_config_buyer_id", schema: "buyersystem", table: "sila_culinary_financial_config", column: "buyer_id", unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_event_financial_plan_buyer_id_plan_code", schema: "buyersystem", table: "sila_culinary_event_financial_plan", columns: new[] { "buyer_id", "plan_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_event_financial_plan_buyer_id_event_id_status", schema: "buyersystem", table: "sila_culinary_event_financial_plan", columns: new[] { "buyer_id", "event_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_event_financial_plan_buyer_id_idempotency_key", schema: "buyersystem", table: "sila_culinary_event_financial_plan", columns: new[] { "buyer_id", "idempotency_key" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_event_revenue_line_buyer_id_event_financial_plan_id", schema: "buyersystem", table: "sila_culinary_event_revenue_line", columns: new[] { "buyer_id", "event_financial_plan_id" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_event_expense_line_buyer_id_event_financial_plan_id_category", schema: "buyersystem", table: "sila_culinary_event_expense_line", columns: new[] { "buyer_id", "event_financial_plan_id", "category" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_financial_exception_buyer_id_event_financial_plan_id_resolution_status", schema: "buyersystem", table: "sila_culinary_financial_exception", columns: new[] { "buyer_id", "event_financial_plan_id", "resolution_status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_financial_scenario_buyer_id_event_financial_plan_id", schema: "buyersystem", table: "sila_culinary_financial_scenario", columns: new[] { "buyer_id", "event_financial_plan_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sila_culinary_financial_scenario", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_financial_exception", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_event_expense_line", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_event_revenue_line", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_event_financial_plan", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_financial_config", schema: "buyersystem");
        }
    }
}
