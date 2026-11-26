using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Phase 0 Recipe Studio 2.0: per-buyer Culinary Intelligence AI Engine configuration and feature flags.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261126230000_SilaCulinaryAiConfiguration")]
    public partial class SilaCulinaryAiConfigurationMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_culinary_ai_configuration",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    operating_mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    allow_external_ai = table.Column<bool>(type: "bit", nullable: false),
                    local_enabled = table.Column<bool>(type: "bit", nullable: false),
                    local_endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    local_model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    gemini_enabled = table.Column<bool>(type: "bit", nullable: false),
                    gemini_model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    protected_gemini_api_key = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    claude_enabled = table.Column<bool>(type: "bit", nullable: false),
                    claude_model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    protected_claude_api_key = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    request_timeout_seconds = table.Column<int>(type: "int", nullable: false),
                    max_output_tokens = table.Column<int>(type: "int", nullable: false),
                    temperature = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    daily_request_limit = table.Column<int>(type: "int", nullable: false),
                    monthly_request_limit = table.Column<int>(type: "int", nullable: false),
                    daily_token_budget = table.Column<int>(type: "int", nullable: false),
                    monthly_cost_budget_usd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    fallback_policy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    task_routing_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    feature_flags_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    last_test_provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    last_test_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    last_test_message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    last_tested_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_culinary_ai_configuration", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_culinary_ai_configuration_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_ai_configuration_buyer_id",
                schema: "buyersystem",
                table: "sila_culinary_ai_configuration",
                column: "buyer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_culinary_ai_configuration_is_active",
                schema: "buyersystem",
                table: "sila_culinary_ai_configuration",
                column: "is_active");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sila_culinary_ai_configuration",
                schema: "buyersystem");
        }
    }
}
