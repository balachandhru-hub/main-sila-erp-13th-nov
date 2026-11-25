using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>
    /// Additive Voice PO session/turn tables and user purchasing profile defaults.
    /// Hand-written Up/Down only — matches RecipeLegacyMigration / GoodsIssueCostCenter pattern.
    /// </summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261123190000_VoicePoUserPurchasingDefaults")]
    public partial class VoicePoUserPurchasingDefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_user_purchasing_profile",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    display_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    profile_version = table.Column<int>(type: "int", nullable: false),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    plant_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    storage_location = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    purchasing_organization = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    purchasing_group = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    default_delivery_location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    account_assignment_category = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    cost_center = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    gl_account = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    material_group = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    profit_center = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    business_area = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    internal_order = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    wbs_element = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    preferred_supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    preferred_supplier_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    default_currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    default_delivery_lead_time_days = table.Column<int>(type: "int", nullable: true),
                    default_po_document_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    default_item_category = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_user_purchasing_profile", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_user_purchasing_profile_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_voice_po_session",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    session_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    purchasing_profile_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    purchasing_profile_version = table.Column<int>(type: "int", nullable: true),
                    latest_transcript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    draft_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    clarification_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    spoken_reply = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    validation_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    validation_errors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    idempotency_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_purchase_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_po_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    language = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    transcript_retain_until = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_voice_po_session", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_voice_po_session_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_voice_po_turn",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    turn_number = table.Column<int>(type: "int", nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    transcript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    spoken_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    structured_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    transcription_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    confidence = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_voice_po_turn", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_voice_po_turn_sila_voice_po_session_session_id",
                        column: x => x.session_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_voice_po_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sila_user_purchasing_profile_is_active",
                schema: "buyersystem",
                table: "sila_user_purchasing_profile",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_user_purchasing_profile_buyer_id_user_id_status",
                schema: "buyersystem",
                table: "sila_user_purchasing_profile",
                columns: new[] { "buyer_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_user_purchasing_profile_buyer_id_user_id_is_default",
                schema: "buyersystem",
                table: "sila_user_purchasing_profile",
                columns: new[] { "buyer_id", "user_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_voice_po_session_is_active",
                schema: "buyersystem",
                table: "sila_voice_po_session",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_voice_po_session_buyer_id_session_number",
                schema: "buyersystem",
                table: "sila_voice_po_session",
                columns: new[] { "buyer_id", "session_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sila_voice_po_session_buyer_id_user_id_status",
                schema: "buyersystem",
                table: "sila_voice_po_session",
                columns: new[] { "buyer_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_voice_po_session_buyer_id_idempotency_key",
                schema: "buyersystem",
                table: "sila_voice_po_session",
                columns: new[] { "buyer_id", "idempotency_key" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_voice_po_turn_is_active",
                schema: "buyersystem",
                table: "sila_voice_po_turn",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_voice_po_turn_session_id_turn_number",
                schema: "buyersystem",
                table: "sila_voice_po_turn",
                columns: new[] { "session_id", "turn_number" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sila_voice_po_turn",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_voice_po_session",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "sila_user_purchasing_profile",
                schema: "buyersystem");
        }
    }
}
