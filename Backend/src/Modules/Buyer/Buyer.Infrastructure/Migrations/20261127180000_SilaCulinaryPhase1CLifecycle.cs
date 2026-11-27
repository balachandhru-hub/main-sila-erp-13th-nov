using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 1C Culinary Intelligence: material requests, consolidation links, history, costing snapshots, recipe lifecycle, and costing config.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261127180000_SilaCulinaryPhase1CLifecycle")]
    public partial class SilaCulinaryPhase1CLifecycleMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "costing_config_json",
                schema: "buyersystem",
                table: "sila_culinary_ai_configuration",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sila_culinary_material_request",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    request_reference = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    request_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    revision_number = table.Column<int>(type: "int", nullable: false),
                    approved_revision_number = table.Column<int>(type: "int", nullable: true),
                    proposed_description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    long_description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ingredient_category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    material_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    material_group = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    product_hierarchy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    brand = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    manufacturer = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    product_specification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    barcode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    country_of_origin = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    plant_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    storage_location = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    purchasing_organization = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    purchasing_group = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    base_uom = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    purchasing_uom = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    alternate_uom = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    pack_size = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    conversion_numerator = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    conversion_denominator = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    net_weight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    gross_weight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    volume = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ingredient_form = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    storage_requirements = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    shelf_life_days = table.Column<int>(type: "int", nullable: true),
                    batch_managed = table.Column<bool>(type: "bit", nullable: false),
                    potential_allergens = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    dietary_classification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    quality_specifications = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    food_safety_requirements = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    required_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    required_date_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    requested_by_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    business_justification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    suggested_supplier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    target_material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    target_material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    data_origin = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    duplicate_check_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    attachments_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    approval_trail_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    execution_reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    execution_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    resulting_material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    resulting_material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    decision_comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    submitted_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approved_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    material_available_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_material_request", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_request_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_material_request_link",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    draft_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ingredient_temp_ref = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ingredient_description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    required_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    recipe_uom = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    reconnected = table.Column<bool>(type: "bit", nullable: false),
                    chef_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    reconnected_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_material_request_link", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_request_link_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_request_link_sila_culinary_material_request_material_request_id",
                        column: x => x.material_request_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_material_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_material_request_history",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    action = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    previous_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    new_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_material_request_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_request_history_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_request_history_sila_culinary_material_request_material_request_id",
                        column: x => x.material_request_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_material_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_costing_snapshot",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    costing_reference = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    draft_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    draft_revision = table.Column<int>(type: "int", nullable: true),
                    production_recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_version = table.Column<int>(type: "int", nullable: true),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    scenario_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    is_baseline = table.Column<bool>(type: "bit", nullable: false),
                    is_approved_snapshot = table.Column<bool>(type: "bit", nullable: false),
                    approval_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    costing_basis = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    tax_basis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    servings = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    total_ingredient_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    sub_recipe_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    additional_direct_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    allocated_overhead_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    total_recipe_cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ingredient_cost_per_portion = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    total_cost_per_portion = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    selling_price_per_portion = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    food_cost_percent = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    target_food_cost_percent = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    recommended_selling_price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    estimated_revenue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    contribution_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    cost_completeness = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    missing_price_count = table.Column<int>(type: "int", nullable: false),
                    lines_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    additional_costs_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    assumptions_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_costing_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_costing_snapshot_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_recipe_lifecycle",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    draft_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approved_draft_revision = table.Column<int>(type: "int", nullable: false),
                    costing_snapshot_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    production_recipe_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    production_recipe_version = table.Column<int>(type: "int", nullable: true),
                    lifecycle_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    image_assigned = table.Column<bool>(type: "bit", nullable: false),
                    image_origin = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    approval_comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    change_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_by_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    approved_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    published_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    effective_date_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    sub_recipe_dependencies_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    readiness_summary_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_recipe_lifecycle", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_recipe_lifecycle_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_request_is_active",
                schema: "buyersystem",
                table: "sila_culinary_material_request",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_request_buyer_id_request_reference",
                schema: "buyersystem",
                table: "sila_culinary_material_request",
                columns: new[] { "buyer_id", "request_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_request_buyer_id_status",
                schema: "buyersystem",
                table: "sila_culinary_material_request",
                columns: new[] { "buyer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_request_link_is_active",
                schema: "buyersystem",
                table: "sila_culinary_material_request_link",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_request_link_buyer_id_material_request_id_draft_id_ingredient_temp_ref",
                schema: "buyersystem",
                table: "sila_culinary_material_request_link",
                columns: new[] { "buyer_id", "material_request_id", "draft_id", "ingredient_temp_ref" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_request_history_is_active",
                schema: "buyersystem",
                table: "sila_culinary_material_request_history",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_request_history_buyer_id_material_request_id",
                schema: "buyersystem",
                table: "sila_culinary_material_request_history",
                columns: new[] { "buyer_id", "material_request_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_costing_snapshot_is_active",
                schema: "buyersystem",
                table: "sila_culinary_costing_snapshot",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_costing_snapshot_buyer_id_costing_reference",
                schema: "buyersystem",
                table: "sila_culinary_costing_snapshot",
                columns: new[] { "buyer_id", "costing_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_costing_snapshot_buyer_id_draft_id",
                schema: "buyersystem",
                table: "sila_culinary_costing_snapshot",
                columns: new[] { "buyer_id", "draft_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_recipe_lifecycle_is_active",
                schema: "buyersystem",
                table: "sila_culinary_recipe_lifecycle",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_recipe_lifecycle_buyer_id_draft_id",
                schema: "buyersystem",
                table: "sila_culinary_recipe_lifecycle",
                columns: new[] { "buyer_id", "draft_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_recipe_lifecycle_buyer_id_production_recipe_id",
                schema: "buyersystem",
                table: "sila_culinary_recipe_lifecycle",
                columns: new[] { "buyer_id", "production_recipe_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sila_culinary_material_request_history",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_culinary_material_request_link",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_culinary_material_request",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_culinary_costing_snapshot",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_culinary_recipe_lifecycle",
                schema: "buyersystem");

            migrationBuilder.DropColumn(
                name: "costing_config_json",
                schema: "buyersystem",
                table: "sila_culinary_ai_configuration");
        }
    }
}
