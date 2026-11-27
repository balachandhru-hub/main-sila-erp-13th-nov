using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase ME.2 additive schema for AI-native agent runs, recommendation cockpit, proactive alerts, learning outcomes, model evaluations &amp; AI feedback.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261201010000_MenuEngineeringAiNativePhase2")]
    public partial class MenuEngineeringAiNativePhase2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "default_operating_mode", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "AI_ASSISTED");
            migrationBuilder.AddColumn<bool>(name: "proactive_alerts_enabled", schema: "buyersystem", table: "menu_engineering_settings", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<int>(name: "proactive_cooldown_minutes", schema: "buyersystem", table: "menu_engineering_settings", type: "int", nullable: false, defaultValue: 120);

            migrationBuilder.CreateTable(
                name: "menu_engineering_agent_run",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    agent_id = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    operating_mode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    menu_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_draft_menu_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    objective_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    optimization_mode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    step_count = table.Column<int>(type: "int", nullable: false),
                    current_step = table.Column<int>(type: "int", nullable: false),
                    steps_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    proposal_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    error_message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_agent_run", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_agent_run_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_recommendation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recommendation_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    agent_id = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    target_object_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    target_object_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    target_object_label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    current_baseline = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    proposed_change = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    supporting_evidence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    estimated_cost_impact = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    estimated_margin_impact = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    realized_financial_impact = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    operational_impact = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    data_quality = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    confidence = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    responsible_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    requires_approval = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    edited_proposal_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    realized_outcome_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    feedback_rating = table.Column<int>(type: "int", nullable: true),
                    feedback_notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    deduplication_key = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    cooldown_until_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    validated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_recommendation", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_recommendation_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_proactive_alert",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    trigger_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    deduplication_key = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    affected_object_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    affected_object_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    metrics_delta_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    recommended_action = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    acknowledged_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    acknowledged_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cooldown_until_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_proactive_alert", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_proactive_alert_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_learning_outcome",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    dataset_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    model_version = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    menu_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    menu_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recommendation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    predicted_food_cost_percent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    actual_food_cost_percent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    predicted_covers = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_covers = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    predicted_margin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_margin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_waste_percent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    outcome_validated = table.Column<bool>(type: "bit", nullable: false),
                    outcome_source = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    features_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    captured_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_learning_outcome", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_learning_outcome_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_model_evaluation",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    model_name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    model_version = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    dataset_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    metric_mae = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    metric_wape = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    metric_bias = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    interval_coverage_percent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    is_active_model = table.Column<bool>(type: "bit", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    evaluated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_model_evaluation", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_model_evaluation_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_ai_feedback",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    agent_id = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    context_screen = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    target_entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    rating = table.Column<int>(type: "int", nullable: false),
                    was_accepted = table.Column<bool>(type: "bit", nullable: false),
                    correction_notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    submitted_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_ai_feedback", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_ai_feedback_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "ix_menu_engineering_agent_run_buyer_id_idempotency_key", schema: "buyersystem", table: "menu_engineering_agent_run", columns: new[] { "buyer_id", "idempotency_key" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_recommendation_buyer_id_status_priority", schema: "buyersystem", table: "menu_engineering_recommendation", columns: new[] { "buyer_id", "status", "priority" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_proactive_alert_buyer_id_deduplication_key", schema: "buyersystem", table: "menu_engineering_proactive_alert", columns: new[] { "buyer_id", "deduplication_key" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_learning_outcome_buyer_id_dataset_version_captured_at_utc", schema: "buyersystem", table: "menu_engineering_learning_outcome", columns: new[] { "buyer_id", "dataset_version", "captured_at_utc" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_model_evaluation_buyer_id_model_name_evaluated_at_utc", schema: "buyersystem", table: "menu_engineering_model_evaluation", columns: new[] { "buyer_id", "model_name", "evaluated_at_utc" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_ai_feedback_buyer_id_agent_id_submitted_at_utc", schema: "buyersystem", table: "menu_engineering_ai_feedback", columns: new[] { "buyer_id", "agent_id", "submitted_at_utc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "menu_engineering_agent_run", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_recommendation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_proactive_alert", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_learning_outcome", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_model_evaluation", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_ai_feedback", schema: "buyersystem");
            migrationBuilder.DropColumn(name: "default_operating_mode", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "proactive_alerts_enabled", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "proactive_cooldown_minutes", schema: "buyersystem", table: "menu_engineering_settings");
        }
    }
}
