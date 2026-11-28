using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase ME.4 additive schema for autonomous business events, autonomy policies, action plans, controlled execution, realized benefits, executive briefings, and integration monitoring.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261203010000_MenuEngineeringAutonomousPhase4")]
    public partial class MenuEngineeringAutonomousPhase4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "autonomous_mode_paused", schema: "buyersystem", table: "menu_engineering_settings", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "automated_execution_disabled", schema: "buyersystem", table: "menu_engineering_settings", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "emergency_pause_reason", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "emergency_paused_at_utc", schema: "buyersystem", table: "menu_engineering_settings", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "emergency_paused_by_user_id", schema: "buyersystem", table: "menu_engineering_settings", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<int>(name: "max_concurrent_jobs", schema: "buyersystem", table: "menu_engineering_settings", type: "int", nullable: false, defaultValue: 4);
            migrationBuilder.AddColumn<int>(name: "daily_ai_token_budget", schema: "buyersystem", table: "menu_engineering_settings", type: "int", nullable: false, defaultValue: 250000);
            migrationBuilder.AddColumn<decimal>(name: "monthly_ai_budget_usd", schema: "buyersystem", table: "menu_engineering_settings", type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 100.00m);
            migrationBuilder.AddColumn<bool>(name: "teams_notifications_enabled", schema: "buyersystem", table: "menu_engineering_settings", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "teams_webhook_url", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "event_schedule_config_json", schema: "buyersystem", table: "menu_engineering_settings", type: "nvarchar(max)", nullable: true);

            migrationBuilder.AddColumn<Guid>(name: "action_plan_id", schema: "buyersystem", table: "menu_engineering_recommendation", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<string>(name: "lifecycle_status", schema: "buyersystem", table: "menu_engineering_recommendation", type: "nvarchar(40)", maxLength: 40, nullable: true);
            migrationBuilder.AddColumn<string>(name: "rejection_reason_code", schema: "buyersystem", table: "menu_engineering_recommendation", type: "nvarchar(60)", maxLength: 60, nullable: true);
            migrationBuilder.AddColumn<string>(name: "rejection_reason_detail", schema: "buyersystem", table: "menu_engineering_recommendation", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "superseded_by_id", schema: "buyersystem", table: "menu_engineering_recommendation", type: "uniqueidentifier", nullable: true);

            migrationBuilder.CreateTable(
                name: "menu_engineering_business_event",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_system = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    object_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    object_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    object_label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    effective_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    schema_version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    materiality_score = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    processing_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    next_retry_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    processed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    dead_letter_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    processed_agents_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_business_event", x => x.id);
                    table.ForeignKey(name: "fk_menu_engineering_business_event_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_agent_autonomy_policy",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    agent_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    action_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    autonomy_level = table.Column<int>(type: "int", nullable: false),
                    max_financial_threshold = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    materiality_threshold_percent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    cooldown_minutes = table.Column<int>(type: "int", nullable: false),
                    is_paused = table.Column<bool>(type: "bit", nullable: false),
                    allowed_actions_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    policy_version = table.Column<int>(type: "int", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_agent_autonomy_policy", x => x.id);
                    table.ForeignKey(name: "fk_menu_engineering_agent_autonomy_policy_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_action_plan",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    trigger_event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    deduplication_key = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    business_objective = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    root_cause_analysis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    affected_objects_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    supporting_evidence_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    expected_financial_impact = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    expected_operational_impact = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    risk_level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    required_approvals_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    responsible_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    execution_deadline_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_consolidated = table.Column<bool>(type: "bit", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_action_plan", x => x.id);
                    table.ForeignKey(name: "fk_menu_engineering_action_plan_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_action_item",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    action_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence_order = table.Column<int>(type: "int", nullable: false),
                    depends_on_action_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    agent_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    tool_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    action_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    risk_classification = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    policy_decision = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    policy_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    financial_impact = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    target_object_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    target_object_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    target_object_version = table.Column<int>(type: "int", nullable: false),
                    data_freshness_snapshot_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    payload_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    approval_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approval_channel = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    idempotency_key = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    execution_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    result_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    external_reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    executed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_action_item", x => x.id);
                    table.ForeignKey(name: "fk_menu_engineering_action_item_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "fk_menu_engineering_action_item_menu_engineering_action_plan_action_plan_id", column: x => x.action_plan_id, principalSchema: "buyersystem", principalTable: "menu_engineering_action_plan", principalColumn: "id", onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_realized_benefit",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recommendation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    action_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    benefit_category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    deduplication_key = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    baseline_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    expected_benefit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    raw_actual_outcome = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    adjusted_realized_benefit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    adjustment_factors_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    methodology = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    data_quality = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    validation_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    validated_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    validated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    measurement_start_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    measurement_end_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_realized_benefit", x => x.id);
                    table.ForeignKey(name: "fk_menu_engineering_realized_benefit_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_executive_briefing",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    persona = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    business_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deduplication_key = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    summary_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    delivery_channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    delivery_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    delivered_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_executive_briefing", x => x.id);
                    table.ForeignKey(name: "fk_menu_engineering_executive_briefing_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_integration_monitor_event",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    integration_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    health_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    failure_category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    technical_detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    business_impact = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    affected_agents_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    recommended_action = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    is_suspended = table.Column<bool>(type: "bit", nullable: false),
                    checked_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_integration_monitor_event", x => x.id);
                    table.ForeignKey(name: "fk_menu_engineering_integration_monitor_event_buyer_business_profile_buyer_id", column: x => x.buyer_id, principalSchema: "buyersystem", principalTable: "buyer_business_profile", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "ix_menu_engineering_business_event_buyer_id_idempotency_key", schema: "buyersystem", table: "menu_engineering_business_event", columns: new[] { "buyer_id", "idempotency_key" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_business_event_buyer_id_processing_status_effective_at_utc", schema: "buyersystem", table: "menu_engineering_business_event", columns: new[] { "buyer_id", "processing_status", "effective_at_utc" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_agent_autonomy_policy_buyer_id_property_id_agent_id_action_type", schema: "buyersystem", table: "menu_engineering_agent_autonomy_policy", columns: new[] { "buyer_id", "property_id", "agent_id", "action_type" }, unique: true, filter: "[property_id] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_action_plan_buyer_id_deduplication_key", schema: "buyersystem", table: "menu_engineering_action_plan", columns: new[] { "buyer_id", "deduplication_key" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_action_plan_buyer_id_status", schema: "buyersystem", table: "menu_engineering_action_plan", columns: new[] { "buyer_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_action_item_buyer_id_idempotency_key", schema: "buyersystem", table: "menu_engineering_action_item", columns: new[] { "buyer_id", "idempotency_key" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_action_item_action_plan_id_sequence_order", schema: "buyersystem", table: "menu_engineering_action_item", columns: new[] { "action_plan_id", "sequence_order" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_realized_benefit_buyer_id_deduplication_key", schema: "buyersystem", table: "menu_engineering_realized_benefit", columns: new[] { "buyer_id", "deduplication_key" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_executive_briefing_buyer_id_deduplication_key", schema: "buyersystem", table: "menu_engineering_executive_briefing", columns: new[] { "buyer_id", "deduplication_key" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_integration_monitor_event_buyer_id_integration_type_checked_at_utc", schema: "buyersystem", table: "menu_engineering_integration_monitor_event", columns: new[] { "buyer_id", "integration_type", "checked_at_utc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "menu_engineering_integration_monitor_event", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_executive_briefing", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_realized_benefit", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_action_item", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_action_plan", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_agent_autonomy_policy", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_business_event", schema: "buyersystem");

            migrationBuilder.DropColumn(name: "action_plan_id", schema: "buyersystem", table: "menu_engineering_recommendation");
            migrationBuilder.DropColumn(name: "lifecycle_status", schema: "buyersystem", table: "menu_engineering_recommendation");
            migrationBuilder.DropColumn(name: "rejection_reason_code", schema: "buyersystem", table: "menu_engineering_recommendation");
            migrationBuilder.DropColumn(name: "rejection_reason_detail", schema: "buyersystem", table: "menu_engineering_recommendation");
            migrationBuilder.DropColumn(name: "superseded_by_id", schema: "buyersystem", table: "menu_engineering_recommendation");

            migrationBuilder.DropColumn(name: "autonomous_mode_paused", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "automated_execution_disabled", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "emergency_pause_reason", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "emergency_paused_at_utc", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "emergency_paused_by_user_id", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "max_concurrent_jobs", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "daily_ai_token_budget", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "monthly_ai_budget_usd", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "teams_notifications_enabled", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "teams_webhook_url", schema: "buyersystem", table: "menu_engineering_settings");
            migrationBuilder.DropColumn(name: "event_schedule_config_json", schema: "buyersystem", table: "menu_engineering_settings");
        }
    }
}
