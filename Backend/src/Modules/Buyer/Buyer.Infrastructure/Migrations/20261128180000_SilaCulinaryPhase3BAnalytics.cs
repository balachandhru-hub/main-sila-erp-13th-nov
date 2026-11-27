using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 3B Culinary Intelligence: Analytics Config, Isolated Simulations, Optimization Jobs, Recommendations, Waste Intelligence, and Audits.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261128180000_SilaCulinaryPhase3BAnalytics")]
    public partial class SilaCulinaryPhase3BAnalyticsMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_culinary_analytics_config",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    target_food_cost_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    target_contribution_margin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    popularity_threshold_factor = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    menu_profitability_threshold_override = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ingredient_price_alert_threshold_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    waste_tolerance_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    consumption_variance_tolerance_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    default_forecast_horizon_days = table.Column<int>(type: "int", nullable: false),
                    minimum_historical_data_days = table.Column<int>(type: "int", nullable: false),
                    min_sales_for_classification = table.Column<int>(type: "int", nullable: false),
                    cost_methodology = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    default_currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    approved_substitution_rules_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    nutritional_constraints_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    allergen_restrictions_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    optimization_objectives_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    preferred_ai_provider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    preferred_ai_model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    daily_ai_query_limit = table.Column<int>(type: "int", nullable: false),
                    recommendation_approval_mode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    scheduled_refresh_frequency = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    config_version = table.Column<int>(type: "int", nullable: false),
                    config_history_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_analytics_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_analytics_config_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_simulation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    simulation_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    scenario_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    isolated_from_production = table.Column<bool>(type: "bit", nullable: false),
                    is_shared = table.Column<bool>(type: "bit", nullable: false),
                    parameters_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    results_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    baseline_contribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    simulated_contribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    expected_savings = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_simulation", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_simulation_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_optimization_job",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    job_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    job_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    objective = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    timeout_seconds = table.Column<int>(type: "int", nullable: false),
                    request_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    result_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    error_message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_optimization_job", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_optimization_job_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_recommendation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    outlet_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    recommendation_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    source_engine = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    deduplication_key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    current_situation = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    proposed_action = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    supporting_evidence_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    expected_financial_impact = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    realized_financial_impact = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    data_quality = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    risk_level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    responsible_role = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    valid_from_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    valid_to_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approval_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    decision_comment = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_recommendation", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_recommendation_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_waste_record",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    meal_period = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    waste_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    waste_category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ingredient_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    menu_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    estimated_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    covers_served = table.Column<int>(type: "int", nullable: false),
                    source_channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("pk_sila_culinary_waste_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_waste_record_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_analytics_audit",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    audit_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    summary = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    prompt_injection_blocked = table.Column<bool>(type: "bit", nullable: false),
                    executed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_analytics_audit", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_analytics_audit_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "ix_sila_culinary_analytics_config_buyer_id_property_id", schema: "buyersystem", table: "sila_culinary_analytics_config", columns: new[] { "buyer_id", "property_id" }, unique: true, filter: "[property_id] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_simulation_buyer_id_simulation_code", schema: "buyersystem", table: "sila_culinary_simulation", columns: new[] { "buyer_id", "simulation_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_optimization_job_buyer_id_job_code", schema: "buyersystem", table: "sila_culinary_optimization_job", columns: new[] { "buyer_id", "job_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_optimization_job_buyer_id_status", schema: "buyersystem", table: "sila_culinary_optimization_job", columns: new[] { "buyer_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_recommendation_buyer_id_recommendation_code", schema: "buyersystem", table: "sila_culinary_recommendation", columns: new[] { "buyer_id", "recommendation_code" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_recommendation_buyer_id_deduplication_key", schema: "buyersystem", table: "sila_culinary_recommendation", columns: new[] { "buyer_id", "deduplication_key" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_recommendation_buyer_id_status", schema: "buyersystem", table: "sila_culinary_recommendation", columns: new[] { "buyer_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_waste_record_buyer_id_waste_number", schema: "buyersystem", table: "sila_culinary_waste_record", columns: new[] { "buyer_id", "waste_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_waste_record_buyer_id_idempotency_key", schema: "buyersystem", table: "sila_culinary_waste_record", columns: new[] { "buyer_id", "idempotency_key" });
            migrationBuilder.CreateIndex(name: "ix_sila_culinary_analytics_audit_buyer_id_executed_at_utc", schema: "buyersystem", table: "sila_culinary_analytics_audit", columns: new[] { "buyer_id", "executed_at_utc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sila_culinary_analytics_audit", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_waste_record", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_recommendation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_optimization_job", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_simulation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_analytics_config", schema: "buyersystem");
        }
    }
}
