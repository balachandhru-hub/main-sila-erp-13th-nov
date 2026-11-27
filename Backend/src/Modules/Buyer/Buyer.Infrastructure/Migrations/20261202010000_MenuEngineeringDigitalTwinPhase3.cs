using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase ME.3 additive schema for Culinary Digital Twin snapshots, simulation/optimization runs, model drift events, and scenario lineage.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261202010000_MenuEngineeringDigitalTwinPhase3")]
    public partial class MenuEngineeringDigitalTwinPhase3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "baseline_snapshot_id", schema: "buyersystem", table: "menu_engineering_scenario", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<int>(name: "baseline_snapshot_version", schema: "buyersystem", table: "menu_engineering_scenario", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(name: "calculation_version", schema: "buyersystem", table: "menu_engineering_scenario", type: "nvarchar(40)", maxLength: 40, nullable: true);
            migrationBuilder.AddColumn<string>(name: "model_version", schema: "buyersystem", table: "menu_engineering_scenario", type: "nvarchar(40)", maxLength: 40, nullable: true);
            migrationBuilder.AddColumn<string>(name: "solver_status", schema: "buyersystem", table: "menu_engineering_scenario", type: "nvarchar(40)", maxLength: 40, nullable: true);
            migrationBuilder.AddColumn<int>(name: "pareto_rank", schema: "buyersystem", table: "menu_engineering_scenario", type: "int", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "parent_scenario_id", schema: "buyersystem", table: "menu_engineering_scenario", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<string>(name: "status", schema: "buyersystem", table: "menu_engineering_scenario", type: "nvarchar(30)", maxLength: 30, nullable: true);
            migrationBuilder.AddColumn<string>(name: "delta_overrides_json", schema: "buyersystem", table: "menu_engineering_scenario", type: "nvarchar(max)", nullable: true);

            migrationBuilder.CreateTable(
                name: "menu_engineering_twin_snapshot",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    menu_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    snapshot_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    snapshot_hash = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    calculation_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    model_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    data_quality_score = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    data_quality_label = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    business_effective_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    source_max_updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    data_freshness_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_stale = table.Column<bool>(type: "bit", nullable: false),
                    twin_payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lineage_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    captured_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    captured_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_twin_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_twin_snapshot_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_twin_simulation_run",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    baseline_snapshot_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    menu_plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    scenario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    run_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    random_seed = table.Column<int>(type: "int", nullable: false),
                    iteration_count = table.Column<int>(type: "int", nullable: false),
                    solver_used = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    runtime_ms = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    optimality_gap = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    parameters_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    assumptions_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    result_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    data_quality = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    is_stale = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("pk_menu_engineering_twin_simulation_run", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_twin_simulation_run_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_engineering_model_drift_event",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    model_name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    model_version = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    dataset_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    drift_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    baseline_metric = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    observed_metric = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    champion_model_version = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    challenger_model_version = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    evidence_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    detected_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_engineering_model_drift_event", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_engineering_model_drift_event_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "ix_menu_engineering_twin_snapshot_buyer_id_snapshot_type_captured_at_utc", schema: "buyersystem", table: "menu_engineering_twin_snapshot", columns: new[] { "buyer_id", "snapshot_type", "captured_at_utc" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_twin_snapshot_buyer_id_menu_plan_id_version_number", schema: "buyersystem", table: "menu_engineering_twin_snapshot", columns: new[] { "buyer_id", "menu_plan_id", "version_number" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_twin_simulation_run_buyer_id_idempotency_key", schema: "buyersystem", table: "menu_engineering_twin_simulation_run", columns: new[] { "buyer_id", "idempotency_key" }, unique: true);
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_twin_simulation_run_buyer_id_run_type_status", schema: "buyersystem", table: "menu_engineering_twin_simulation_run", columns: new[] { "buyer_id", "run_type", "status" });
            migrationBuilder.CreateIndex(name: "ix_menu_engineering_model_drift_event_buyer_id_model_name_detected_at_utc", schema: "buyersystem", table: "menu_engineering_model_drift_event", columns: new[] { "buyer_id", "model_name", "detected_at_utc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "menu_engineering_twin_snapshot", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_twin_simulation_run", schema: "buyersystem");
            migrationBuilder.DropTable(name: "menu_engineering_model_drift_event", schema: "buyersystem");
            migrationBuilder.DropColumn(name: "baseline_snapshot_id", schema: "buyersystem", table: "menu_engineering_scenario");
            migrationBuilder.DropColumn(name: "baseline_snapshot_version", schema: "buyersystem", table: "menu_engineering_scenario");
            migrationBuilder.DropColumn(name: "calculation_version", schema: "buyersystem", table: "menu_engineering_scenario");
            migrationBuilder.DropColumn(name: "model_version", schema: "buyersystem", table: "menu_engineering_scenario");
            migrationBuilder.DropColumn(name: "solver_status", schema: "buyersystem", table: "menu_engineering_scenario");
            migrationBuilder.DropColumn(name: "pareto_rank", schema: "buyersystem", table: "menu_engineering_scenario");
            migrationBuilder.DropColumn(name: "parent_scenario_id", schema: "buyersystem", table: "menu_engineering_scenario");
            migrationBuilder.DropColumn(name: "status", schema: "buyersystem", table: "menu_engineering_scenario");
            migrationBuilder.DropColumn(name: "delta_overrides_json", schema: "buyersystem", table: "menu_engineering_scenario");
        }
    }
}
