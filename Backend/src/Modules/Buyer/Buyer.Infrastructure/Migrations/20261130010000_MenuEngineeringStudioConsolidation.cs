using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Additive schema consolidation for ME Studio, Menu Items, Planning, Portion, Demand, POS Sales/Mapping, Scenarios, Approvals &amp; Setup.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261130010000_MenuEngineeringStudioConsolidation")]
    public partial class MenuEngineeringStudioConsolidation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. MenuItem additive columns
            migrationBuilder.AddColumn<string>(name: "alternative_name", schema: "buyersystem", table: "menu_item", type: "nvarchar(200)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "service_format", schema: "buyersystem", table: "menu_item", type: "nvarchar(60)", nullable: true);
            migrationBuilder.AddColumn<int>(name: "approved_recipe_version", schema: "buyersystem", table: "menu_item", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(name: "recipe_combination_json", schema: "buyersystem", table: "menu_item", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "tax_rate_percent", schema: "buyersystem", table: "menu_item", type: "decimal(9,4)", precision: 9, scale: 4, nullable: true);
            migrationBuilder.AddColumn<bool>(name: "tax_inclusive", schema: "buyersystem", table: "menu_item", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "pos_item_reference", schema: "buyersystem", table: "menu_item", type: "nvarchar(120)", nullable: true);
            migrationBuilder.AddColumn<int>(name: "prep_time_minutes", schema: "buyersystem", table: "menu_item", type: "int", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "is_available", schema: "buyersystem", table: "menu_item", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<string>(name: "seasonal_availability", schema: "buyersystem", table: "menu_item", type: "nvarchar(120)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "approval_status", schema: "buyersystem", table: "menu_item", type: "nvarchar(40)", nullable: false, defaultValue: "DRAFT");
            migrationBuilder.AddColumn<int>(name: "version_number", schema: "buyersystem", table: "menu_item", type: "int", nullable: false, defaultValue: 1);

            // 2. MenuPlan additive columns
            migrationBuilder.AddColumn<string>(name: "menu_type", schema: "buyersystem", table: "menu_plan", type: "nvarchar(60)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "service_format", schema: "buyersystem", table: "menu_plan", type: "nvarchar(60)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "venue_name", schema: "buyersystem", table: "menu_plan", type: "nvarchar(160)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "rotation_rule", schema: "buyersystem", table: "menu_plan", type: "nvarchar(160)", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "is_template", schema: "buyersystem", table: "menu_plan", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "template_category", schema: "buyersystem", table: "menu_plan", type: "nvarchar(80)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "target_food_cost_percent", schema: "buyersystem", table: "menu_plan", type: "decimal(9,4)", precision: 9, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "target_contribution_margin", schema: "buyersystem", table: "menu_plan", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<string>(name: "dietary_requirements", schema: "buyersystem", table: "menu_plan", type: "nvarchar(400)", nullable: true);
            migrationBuilder.AddColumn<int>(name: "version_number", schema: "buyersystem", table: "menu_plan", type: "int", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<Guid>(name: "parent_published_plan_id", schema: "buyersystem", table: "menu_plan", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<string>(name: "source_type", schema: "buyersystem", table: "menu_plan", type: "nvarchar(60)", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "source_reference_id", schema: "buyersystem", table: "menu_plan", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "submitted_by_user_id", schema: "buyersystem", table: "menu_plan", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "submitted_at_utc", schema: "buyersystem", table: "menu_plan", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "approved_by_user_id", schema: "buyersystem", table: "menu_plan", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "approved_at_utc", schema: "buyersystem", table: "menu_plan", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "published_by_user_id", schema: "buyersystem", table: "menu_plan", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "published_at_utc", schema: "buyersystem", table: "menu_plan", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "approval_comment", schema: "buyersystem", table: "menu_plan", type: "nvarchar(500)", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "prevent_conflicts", schema: "buyersystem", table: "menu_plan", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<string>(name: "categories_order_json", schema: "buyersystem", table: "menu_plan", type: "nvarchar(max)", nullable: true);

            // 3. MenuPlanLine additive columns
            migrationBuilder.AddColumn<string>(name: "category_name", schema: "buyersystem", table: "menu_plan_line", type: "nvarchar(100)", nullable: true);
            migrationBuilder.AddColumn<int>(name: "sequence_order", schema: "buyersystem", table: "menu_plan_line", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<Guid>(name: "recipe_id", schema: "buyersystem", table: "menu_plan_line", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<int>(name: "recipe_version", schema: "buyersystem", table: "menu_plan_line", type: "int", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "portion_size", schema: "buyersystem", table: "menu_plan_line", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<string>(name: "portion_uom", schema: "buyersystem", table: "menu_plan_line", type: "nvarchar(30)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "expected_servings_per_guest", schema: "buyersystem", table: "menu_plan_line", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "selling_price", schema: "buyersystem", table: "menu_plan_line", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "expected_covers", schema: "buyersystem", table: "menu_plan_line", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "sampled_actual_portion", schema: "buyersystem", table: "menu_plan_line", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<string>(name: "line_notes", schema: "buyersystem", table: "menu_plan_line", type: "nvarchar(500)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "alternatives_json", schema: "buyersystem", table: "menu_plan_line", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "dietary_tags", schema: "buyersystem", table: "menu_plan_line", type: "nvarchar(400)", nullable: true);

            // 4. MenuPosMapping additive columns
            migrationBuilder.AddColumn<Guid>(name: "recipe_id", schema: "buyersystem", table: "menu_pos_mapping", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<int>(name: "approved_recipe_version", schema: "buyersystem", table: "menu_pos_mapping", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(name: "mapping_type", schema: "buyersystem", table: "menu_pos_mapping", type: "nvarchar(40)", nullable: false, defaultValue: "ONE_TO_ONE");
            migrationBuilder.AddColumn<decimal>(name: "portion_multiplier", schema: "buyersystem", table: "menu_pos_mapping", type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1m);
            migrationBuilder.AddColumn<string>(name: "uom_code", schema: "buyersystem", table: "menu_pos_mapping", type: "nvarchar(30)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "uom_conversion_factor", schema: "buyersystem", table: "menu_pos_mapping", type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1m);
            migrationBuilder.AddColumn<string>(name: "modifier_code", schema: "buyersystem", table: "menu_pos_mapping", type: "nvarchar(80)", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "modifier_recipe_id", schema: "buyersystem", table: "menu_pos_mapping", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "modifier_portion_multiplier", schema: "buyersystem", table: "menu_pos_mapping", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<string>(name: "combo_parent_code", schema: "buyersystem", table: "menu_pos_mapping", type: "nvarchar(80)", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "is_combo_component", schema: "buyersystem", table: "menu_pos_mapping", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "exclude_component_double_count", schema: "buyersystem", table: "menu_pos_mapping", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<Guid>(name: "approved_by_user_id", schema: "buyersystem", table: "menu_pos_mapping", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "approved_at_utc", schema: "buyersystem", table: "menu_pos_mapping", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "mapping_notes", schema: "buyersystem", table: "menu_pos_mapping", type: "nvarchar(500)", nullable: true);

            // 5. PosSalesTransaction additive columns
            migrationBuilder.AddColumn<string>(name: "pos_line_id", schema: "buyersystem", table: "pos_sales_transaction", type: "nvarchar(100)", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "property_id", schema: "buyersystem", table: "pos_sales_transaction", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "transaction_timestamp_utc", schema: "buyersystem", table: "pos_sales_transaction", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "gross_selling_price", schema: "buyersystem", table: "pos_sales_transaction", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "discount_amount", schema: "buyersystem", table: "pos_sales_transaction", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "tax_amount", schema: "buyersystem", table: "pos_sales_transaction", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "net_revenue", schema: "buyersystem", table: "pos_sales_transaction", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<bool>(name: "is_void", schema: "buyersystem", table: "pos_sales_transaction", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "is_refund", schema: "buyersystem", table: "pos_sales_transaction", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "is_complimentary", schema: "buyersystem", table: "pos_sales_transaction", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "modifier_code", schema: "buyersystem", table: "pos_sales_transaction", type: "nvarchar(80)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "combo_reference", schema: "buyersystem", table: "pos_sales_transaction", type: "nvarchar(100)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "meal_period", schema: "buyersystem", table: "pos_sales_transaction", type: "nvarchar(60)", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "menu_item_id", schema: "buyersystem", table: "pos_sales_transaction", type: "uniqueidentifier", nullable: true);

            // 6. MenuEngineeringSettings additive columns
            migrationBuilder.AddColumn<bool>(name: "menu_engineering_enabled", schema: "buyersystem", table: "menu_engineering_settings", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<bool>(name: "me_studio_enabled", schema: "buyersystem", table: "menu_engineering_settings", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<Guid>(name: "property_id", schema: "buyersystem", table: "menu_engineering_settings", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<string>(name: "service_formats_json", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "cuisines_json", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "dietary_tags_json", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "costing_method", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(40)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "target_food_cost_percent", schema: "buyersystem", table: "menu_engineering_settings", type: "decimal(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 0.30m);
            migrationBuilder.AddColumn<decimal>(name: "target_contribution_margin", schema: "buyersystem", table: "menu_engineering_settings", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<string>(name: "cost_refresh_frequency", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(30)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "default_currency", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(10)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "tax_treatment", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(30)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "default_portion_uom", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(30)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "portion_variance_threshold_percent", schema: "buyersystem", table: "menu_engineering_settings", type: "decimal(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 10m);
            migrationBuilder.AddColumn<int>(name: "forecast_horizon_days", schema: "buyersystem", table: "menu_engineering_settings", type: "int", nullable: false, defaultValue: 14);
            migrationBuilder.AddColumn<string>(name: "forecast_method", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(40)", nullable: true);
            migrationBuilder.AddColumn<int>(name: "historical_lookback_days", schema: "buyersystem", table: "menu_engineering_settings", type: "int", nullable: false, defaultValue: 90);
            migrationBuilder.AddColumn<decimal>(name: "forecast_accuracy_threshold_percent", schema: "buyersystem", table: "menu_engineering_settings", type: "decimal(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 80m);
            migrationBuilder.AddColumn<string>(name: "profitability_basis", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(40)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "profitability_threshold_percent", schema: "buyersystem", table: "menu_engineering_settings", type: "decimal(9,4)", precision: 9, scale: 4, nullable: true);
            migrationBuilder.AddColumn<int>(name: "minimum_sales_history_days", schema: "buyersystem", table: "menu_engineering_settings", type: "int", nullable: false, defaultValue: 14);
            migrationBuilder.AddColumn<decimal>(name: "minimum_sales_quantity", schema: "buyersystem", table: "menu_engineering_settings", type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 10m);
            migrationBuilder.AddColumn<string>(name: "comparable_grouping", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(40)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "pos_sync_schedule_cron", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(60)", nullable: true);
            migrationBuilder.AddColumn<int>(name: "pos_retry_max_attempts", schema: "buyersystem", table: "menu_engineering_settings", type: "int", nullable: false, defaultValue: 3);
            migrationBuilder.AddColumn<string>(name: "ai_enabled_agents_json", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "ai_confidence_threshold", schema: "buyersystem", table: "menu_engineering_settings", type: "decimal(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 0.70m);
            migrationBuilder.AddColumn<int>(name: "ai_usage_limit_per_day", schema: "buyersystem", table: "menu_engineering_settings", type: "int", nullable: false, defaultValue: 100);
            migrationBuilder.AddColumn<decimal>(name: "approval_food_cost_threshold_percent", schema: "buyersystem", table: "menu_engineering_settings", type: "decimal(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 35m);
            migrationBuilder.AddColumn<decimal>(name: "approval_price_change_percent", schema: "buyersystem", table: "menu_engineering_settings", type: "decimal(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 10m);
            migrationBuilder.AddColumn<bool>(name: "require_approval_on_substitution", schema: "buyersystem", table: "menu_engineering_settings", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<bool>(name: "require_approval_on_publish", schema: "buyersystem", table: "menu_engineering_settings", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<string>(name: "approval_roles_json", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<int>(name: "config_version", schema: "buyersystem", table: "menu_engineering_settings", type: "int", nullable: false, defaultValue: 1);

            // 7. New tables
            migrationBuilder.CreateTable(
                name: "menu_item_version",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    selling_price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ingredient_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    linked_recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_recipe_version = table.Column<int>(type: "int", nullable: true),
                    change_summary = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    action_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    action_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_item_version", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_item_version_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_scenario",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    menu_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    scenario_name = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    scenario_type = table.Column<string>(type: "nvarchar(60)", nullable: false),
                    variables_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    baseline_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    result_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fixed_demand_disclosed = table.Column<bool>(type: "bit", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    shared_with_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_preferred = table.Column<bool>(type: "bit", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_scenario", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_scenario_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_approval_history",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    from_status = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    to_status = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    action_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approver_role = table.Column<string>(type: "nvarchar(80)", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    action_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_approval_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_approval_history_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_demand_override",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    menu_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    business_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    meal_period = table.Column<string>(type: "nvarchar(60)", nullable: true),
                    baseline_forecast = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ai_adjusted_forecast = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    confirmed_demand = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    override_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    overridden_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    overridden_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_demand_override", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_demand_override_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_config_history",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    config_version = table.Column<int>(type: "int", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    change_summary = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    previous_config_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    new_config_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_config_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_config_history_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "ix_menu_item_version_buyer_id_menu_item_id_version_number", schema: "buyersystem", table: "menu_item_version", columns: new[] { "buyer_id", "menu_item_id", "version_number" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_scenario_buyer_id_menu_plan_id", schema: "buyersystem", table: "menu_engineering_scenario", columns: new[] { "buyer_id", "menu_plan_id" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_approval_history_buyer_id_entity_type_entity_id_action_at_utc", schema: "buyersystem", table: "menu_engineering_approval_history", columns: new[] { "buyer_id", "entity_type", "entity_id", "action_at_utc" });
            migrationBuilder.CreateIndex(name: "ix_menu_demand_override_buyer_id_business_date_outlet_location_id", schema: "buyersystem", table: "menu_demand_override", columns: new[] { "buyer_id", "business_date", "outlet_location_id" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_config_history_buyer_id_property_id_config_version", schema: "buyersystem", table: "menu_engineering_config_history", columns: new[] { "buyer_id", "property_id", "config_version" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "menu_item_version", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_scenario", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_approval_history", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_demand_override", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_config_history", schema: "buyersystem");
        }
    }
}
