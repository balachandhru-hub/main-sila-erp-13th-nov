using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 1B Material Intelligence: mappings, history, tenant aliases, feedback, index state, and AI material config.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261127120000_SilaCulinaryMaterialIntelligence")]
    public partial class SilaCulinaryMaterialIntelligenceMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "material_config_json",
                schema: "buyersystem",
                table: "sila_culinary_ai_configuration",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sila_culinary_material_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    draft_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    draft_revision = table.Column<int>(type: "int", nullable: false),
                    ingredient_temp_ref = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    original_description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    normalized_description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    material_description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    material_revision_stamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    recipe_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    recipe_uom = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    converted_base_quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    base_uom = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    conversion_factor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    match_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    match_score = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    mapping_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    missing_classification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    recommended_request_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    specification_compatible = table.Column<bool>(type: "bit", nullable: false),
                    uom_compatible = table.Column<bool>(type: "bit", nullable: false),
                    stock_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    price_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    is_substitution = table.Column<bool>(type: "bit", nullable: false),
                    substituted_from_material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    replacement_ratio = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    override_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    missing_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    evidence_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    candidates_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    request_draft_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    mapping_revision = table.Column<int>(type: "int", nullable: false),
                    confirmed_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    confirmed_by_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    confirmed_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_matched_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_material_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_mapping_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_mapping_sila_culinary_recipe_draft_draft_id",
                        column: x => x.draft_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_recipe_draft",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_material_mapping_history",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mapping_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    draft_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ingredient_temp_ref = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    mapping_revision = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    previous_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    new_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    previous_material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    new_material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    payload_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_material_mapping_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_mapping_history_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_material_alias",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    alias_text = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    normalized_alias = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    language = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    approval_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    revision = table.Column<int>(type: "int", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_by_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    approved_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    effective_from_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_material_alias", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_alias_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_material_feedback",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    draft_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ingredient_temp_ref = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ingredient_description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    suggested_material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    suggested_material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    selected_material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    selected_material_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    feedback_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_material_feedback", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_feedback_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_material_index_state",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    indexed_material_count = table.Column<int>(type: "int", nullable: false),
                    active_material_count = table.Column<int>(type: "int", nullable: false),
                    pending_updates_count = table.Column<int>(type: "int", nullable: false),
                    embedding_model_version = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    reranking_model_version = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    semantic_backend = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    build_duration_ms = table.Column<double>(type: "float", nullable: false),
                    last_build_started_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_build_completed_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_incremental_sync_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_material_index_state", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_material_index_state_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_mapping_is_active",
                schema: "buyersystem",
                table: "sila_culinary_material_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_mapping_buyer_id_draft_id_ingredient_temp_ref",
                schema: "buyersystem",
                table: "sila_culinary_material_mapping",
                columns: new[] { "buyer_id", "draft_id", "ingredient_temp_ref" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_mapping_buyer_id_mapping_status",
                schema: "buyersystem",
                table: "sila_culinary_material_mapping",
                columns: new[] { "buyer_id", "mapping_status" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_mapping_history_is_active",
                schema: "buyersystem",
                table: "sila_culinary_material_mapping_history",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_mapping_history_buyer_id_draft_id_ingredient_temp_ref",
                schema: "buyersystem",
                table: "sila_culinary_material_mapping_history",
                columns: new[] { "buyer_id", "draft_id", "ingredient_temp_ref" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_alias_is_active",
                schema: "buyersystem",
                table: "sila_culinary_material_alias",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_alias_buyer_id_normalized_alias_property_id",
                schema: "buyersystem",
                table: "sila_culinary_material_alias",
                columns: new[] { "buyer_id", "normalized_alias", "property_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_alias_buyer_id_approval_status",
                schema: "buyersystem",
                table: "sila_culinary_material_alias",
                columns: new[] { "buyer_id", "approval_status" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_feedback_is_active",
                schema: "buyersystem",
                table: "sila_culinary_material_feedback",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_feedback_buyer_id_feedback_type",
                schema: "buyersystem",
                table: "sila_culinary_material_feedback",
                columns: new[] { "buyer_id", "feedback_type" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_index_state_is_active",
                schema: "buyersystem",
                table: "sila_culinary_material_index_state",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_material_index_state_buyer_id",
                schema: "buyersystem",
                table: "sila_culinary_material_index_state",
                column: "buyer_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sila_culinary_material_mapping_history",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_culinary_material_mapping",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_culinary_material_alias",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_culinary_material_feedback",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_culinary_material_index_state",
                schema: "buyersystem");

            migrationBuilder.DropColumn(
                name: "material_config_json",
                schema: "buyersystem",
                table: "sila_culinary_ai_configuration");
        }
    }
}
