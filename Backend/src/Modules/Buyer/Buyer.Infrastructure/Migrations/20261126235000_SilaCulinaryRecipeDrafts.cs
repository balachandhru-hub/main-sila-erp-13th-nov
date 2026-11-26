using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 1A Culinary Intelligence: AI recipe drafts, revisions, and usage telemetry logs.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261126235000_SilaCulinaryRecipeDrafts")]
    public partial class SilaCulinaryRecipeDraftsMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_culinary_recipe_draft",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    draft_reference = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    recipe_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    generation_mode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kitchen_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    source_recipe_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_document_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    base_servings = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ingredient_count = table.Column<int>(type: "int", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    prompt_version = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    generation_reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    validation_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    revision_number = table.Column<int>(type: "int", nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    validation_warnings_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conversation_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    total_input_tokens = table.Column<long>(type: "bigint", nullable: false),
                    total_output_tokens = table.Column<long>(type: "bigint", nullable: false),
                    estimated_cost_usd = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_recipe_draft", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_recipe_draft_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_recipe_revision",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    draft_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    revision_number = table.Column<int>(type: "int", nullable: false),
                    change_type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    change_summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("pk_sila_culinary_recipe_revision", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_recipe_revision_sila_culinary_recipe_draft_draft_id",
                        column: x => x.draft_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_culinary_recipe_draft",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_culinary_ai_usage_log",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    draft_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    draft_reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    generation_reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    feature = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    success = table.Column<bool>(type: "bit", nullable: false),
                    used_fallback = table.Column<bool>(type: "bit", nullable: false),
                    input_tokens = table.Column<int>(type: "int", nullable: false),
                    output_tokens = table.Column<int>(type: "int", nullable: false),
                    total_tokens = table.Column<int>(type: "int", nullable: false),
                    estimated_cost_usd = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    latency_ms = table.Column<int>(type: "int", nullable: false),
                    validation_retries = table.Column<int>(type: "int", nullable: false),
                    error_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    sanitized_error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_ai_usage_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_ai_usage_log_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_recipe_draft_is_active",
                schema: "buyersystem",
                table: "sila_culinary_recipe_draft",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_recipe_draft_buyer_id_draft_reference",
                schema: "buyersystem",
                table: "sila_culinary_recipe_draft",
                columns: new[] { "buyer_id", "draft_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_recipe_draft_buyer_id_status",
                schema: "buyersystem",
                table: "sila_culinary_recipe_draft",
                columns: new[] { "buyer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_recipe_revision_is_active",
                schema: "buyersystem",
                table: "sila_culinary_recipe_revision",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_recipe_revision_draft_id_revision_number",
                schema: "buyersystem",
                table: "sila_culinary_recipe_revision",
                columns: new[] { "draft_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_ai_usage_log_is_active",
                schema: "buyersystem",
                table: "sila_culinary_ai_usage_log",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_ai_usage_log_buyer_id_date_created",
                schema: "buyersystem",
                table: "sila_culinary_ai_usage_log",
                columns: new[] { "buyer_id", "date_created" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sila_culinary_recipe_revision",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_culinary_ai_usage_log",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_culinary_recipe_draft",
                schema: "buyersystem");
        }
    }
}
