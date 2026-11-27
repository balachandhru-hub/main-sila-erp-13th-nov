using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 2A Culinary Intelligence: Event Master, Sessions, Templates, Menu Studio, Menu Lines, and Event Revisions.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261127220000_SilaCulinaryPhase2AEventMenu")]
    public partial class SilaCulinaryPhase2AEventMenuMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_culinary_event",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    event_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    event_category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    customer_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    customer_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    booking_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    external_event_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    event_coordinator = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    responsible_chef = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    executive_chef = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    banquet_manager = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    revision_number = table.Column<int>(type: "int", nullable: false),
                    approved_revision_number = table.Column<int>(type: "int", nullable: true),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    plant_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    venue_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    outlet_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    storage_location_codes_json = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    start_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    start_time = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    end_time = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    prep_start_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    menu_confirmation_deadline_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    guest_confirmation_deadline_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    mrp_deadline_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    time_zone_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    guaranteed_guests = table.Column<int>(type: "int", nullable: false),
                    expected_guests = table.Column<int>(type: "int", nullable: false),
                    max_capacity = table.Column<int>(type: "int", nullable: true),
                    additional_allowance_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    planning_guests = table.Column<int>(type: "int", nullable: false),
                    billable_guests = table.Column<int>(type: "int", nullable: false),
                    billable_policy = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    adults_count = table.Column<int>(type: "int", nullable: false),
                    children_count = table.Column<int>(type: "int", nullable: false),
                    vip_count = table.Column<int>(type: "int", nullable: false),
                    staff_meals_count = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    net_price_per_guest = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    fixed_package_price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    estimated_food_revenue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    estimated_beverage_revenue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    other_planned_revenue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    target_food_cost_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    target_beverage_cost_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    combined_menu_budget = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    approved_budget = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    service_style = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    primary_menu_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    wizard_step_completed = table.Column<int>(type: "int", nullable: false),
                    dietary_requirements_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    dietary_review_verified = table.Column<bool>(type: "bit", nullable: false),
                    dietary_reviewer_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    dietary_verified_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    readiness_summary_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    approved_baseline_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    submitted_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    submitted_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approval_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    approval_decision_comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_event", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_event_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_event_session",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    session_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    day_number = table.Column<int>(type: "int", nullable: false),
                    session_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    start_time = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    end_time = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    session_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    venue_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    kitchen_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    service_style = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    guest_count = table.Column<int>(type: "int", nullable: false),
                    menu_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    budget_allocation = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    prep_deadline_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    dietary_notes_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_event_session", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_event_session_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_event_template",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    template_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    event_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    default_service_style = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    default_duration_days = table.Column<int>(type: "int", nullable: false),
                    default_allowance_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    default_target_food_cost_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    template_payload_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_event_template", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_event_template_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_menu",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    menu_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    menu_category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    cuisine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    service_style = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    effective_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    menu_image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    is_reusable_template = table.Column<bool>(type: "bit", nullable: false),
                    is_scenario = table.Column<bool>(type: "bit", nullable: false),
                    scenario_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    is_selected_scenario = table.Column<bool>(type: "bit", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    target_selling_price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    target_food_cost_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    total_food_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    total_beverage_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    total_estimated_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    cost_per_guest = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    dietary_summary_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    allergen_matrix_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ai_recommendation_notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_menu", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_menu_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_menu_line",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    menu_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    course_name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    item_role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    is_locked = table.Column<bool>(type: "bit", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    recipe_version = table.Column<int>(type: "int", nullable: true),
                    recipe_draft_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    dish_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    customer_description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    dish_image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    line_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    service_style = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    uptake_percent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    serving_frequency = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    portion_size = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    portion_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    allocated_portions = table.Column<int>(type: "int", nullable: true),
                    manual_portions_override = table.Column<int>(type: "int", nullable: true),
                    planned_portions = table.Column<int>(type: "int", nullable: false),
                    costing_snapshot_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    unit_cost_per_portion = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    estimated_line_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    dietary_tags_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    verified_allergens_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    allergen_unknown = table.Column<bool>(type: "bit", nullable: false),
                    allergen_verified = table.Column<bool>(type: "bit", nullable: false),
                    is_beverage = table.Column<bool>(type: "bit", nullable: false),
                    servings_per_guest = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    serving_volume_ml = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    container_size_ml = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    live_station_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    verified_station_capacity_per_hour = table.Column<int>(type: "int", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_menu_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_menu_line_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_event_revision",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    revision_number = table.Column<int>(type: "int", nullable: false),
                    status_at_revision = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    is_approved_baseline = table.Column<bool>(type: "bit", nullable: false),
                    change_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    guaranteed_guests_at_revision = table.Column<int>(type: "int", nullable: false),
                    planning_guests_at_revision = table.Column<int>(type: "int", nullable: false),
                    menu_cost_at_revision = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    approval_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_event_revision", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_event_revision_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_event_buyer_id_event_code",
                schema: "buyersystem",
                table: "sila_culinary_event",
                columns: new[] { "buyer_id", "event_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_event_buyer_id_start_date_utc_end_date_utc",
                schema: "buyersystem",
                table: "sila_culinary_event",
                columns: new[] { "buyer_id", "start_date_utc", "end_date_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_event_buyer_id_status",
                schema: "buyersystem",
                table: "sila_culinary_event",
                columns: new[] { "buyer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_event_session_buyer_id_event_id_session_date_utc",
                schema: "buyersystem",
                table: "sila_culinary_event_session",
                columns: new[] { "buyer_id", "event_id", "session_date_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_event_template_buyer_id_template_code",
                schema: "buyersystem",
                table: "sila_culinary_event_template",
                columns: new[] { "buyer_id", "template_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_menu_buyer_id_menu_code",
                schema: "buyersystem",
                table: "sila_culinary_menu",
                columns: new[] { "buyer_id", "menu_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_menu_buyer_id_event_id",
                schema: "buyersystem",
                table: "sila_culinary_menu",
                columns: new[] { "buyer_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_menu_line_buyer_id_menu_id_sort_order",
                schema: "buyersystem",
                table: "sila_culinary_menu_line",
                columns: new[] { "buyer_id", "menu_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_event_revision_buyer_id_event_id_revision_number",
                schema: "buyersystem",
                table: "sila_culinary_event_revision",
                columns: new[] { "buyer_id", "event_id", "revision_number" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sila_culinary_event_revision", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_menu_line", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_menu", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_event_template", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_event_session", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_culinary_event", schema: "buyersystem");
        }
    }
}
