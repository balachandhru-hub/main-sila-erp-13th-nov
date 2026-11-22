using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>
    /// Additive Recipe Studio legacy migration staging tables (batch, recipe rows, material maps, images).
    /// Hand-written Up/Down only — matches RecipeHospitalityFields / GoodsIssueCostCenter pattern (no Designer/snapshot).
    /// </summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261122120000_RecipeLegacyMigration")]
    public partial class RecipeLegacyMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recipe_legacy_batch",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    stored_file_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recipe_count = table.Column<int>(type: "int", nullable: false),
                    valid_count = table.Column<int>(type: "int", nullable: false),
                    warning_count = table.Column<int>(type: "int", nullable: false),
                    error_count = table.Column<int>(type: "int", nullable: false),
                    unmapped_count = table.Column<int>(type: "int", nullable: false),
                    duplicate_count = table.Column<int>(type: "int", nullable: false),
                    revision = table.Column<int>(type: "int", nullable: false),
                    approval_comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    approved_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rejected_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    rejected_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    posted_count = table.Column<int>(type: "int", nullable: false),
                    failed_post_count = table.Column<int>(type: "int", nullable: false),
                    image_assigned_count = table.Column<int>(type: "int", nullable: false),
                    published_count = table.Column<int>(type: "int", nullable: false),
                    validation_report_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    approval_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    posting_report_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image_report_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    publish_report_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_legacy_batch", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_legacy_batch_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_legacy_recipe",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    legacy_recipe_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    recipe_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    subcategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    recipe_format = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    product_class = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    yield_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    yield_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    portion_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    portion_uom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    prep_minutes = table.Column<int>(type: "int", nullable: true),
                    cook_minutes = table.Column<int>(type: "int", nullable: true),
                    serving_instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    outlet_scope = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    active_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    legacy_reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ingredients_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    instructions_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sub_recipes_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    variants_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    validation_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    error_count = table.Column<int>(type: "int", nullable: false),
                    warning_count = table.Column<int>(type: "int", nullable: false),
                    findings_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    material_mappings_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_recipe_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    created_version = table.Column<int>(type: "int", nullable: true),
                    posting_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    posting_error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    publish_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    source_row = table.Column<int>(type: "int", nullable: false),
                    is_selected = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_legacy_recipe", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_legacy_recipe_recipe_legacy_batch_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "buyersystem",
                        principalTable: "recipe_legacy_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_legacy_material_map",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    legacy_recipe_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ingredient_line = table.Column<int>(type: "int", nullable: false),
                    legacy_material_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    material_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    material_description = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    mapped_material_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_legacy_material_map", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipe_legacy_image",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipe_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    file_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    stored_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_primary = table.Column<bool>(type: "bit", nullable: false),
                    replace_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_legacy_image", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_legacy_batch_is_active",
                schema: "buyersystem",
                table: "recipe_legacy_batch",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_legacy_batch_buyer_id_batch_number",
                schema: "buyersystem",
                table: "recipe_legacy_batch",
                columns: new[] { "buyer_id", "batch_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_legacy_recipe_is_active",
                schema: "buyersystem",
                table: "recipe_legacy_recipe",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_legacy_recipe_buyer_id_batch_id_legacy_recipe_id",
                schema: "buyersystem",
                table: "recipe_legacy_recipe",
                columns: new[] { "buyer_id", "batch_id", "legacy_recipe_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_legacy_recipe_batch_id_validation_status",
                schema: "buyersystem",
                table: "recipe_legacy_recipe",
                columns: new[] { "batch_id", "validation_status" });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_legacy_material_map_is_active",
                schema: "buyersystem",
                table: "recipe_legacy_material_map",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_legacy_material_map_batch_id_legacy_recipe_id_ingredient_line",
                schema: "buyersystem",
                table: "recipe_legacy_material_map",
                columns: new[] { "batch_id", "legacy_recipe_id", "ingredient_line" });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_legacy_image_is_active",
                schema: "buyersystem",
                table: "recipe_legacy_image",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_legacy_image_batch_id_status",
                schema: "buyersystem",
                table: "recipe_legacy_image",
                columns: new[] { "batch_id", "status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "recipe_legacy_image", schema: "buyersystem");
            migrationBuilder.DropTable(name: "recipe_legacy_material_map", schema: "buyersystem");
            migrationBuilder.DropTable(name: "recipe_legacy_recipe", schema: "buyersystem");
            migrationBuilder.DropTable(name: "recipe_legacy_batch", schema: "buyersystem");
        }
    }
}
