using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <summary>Flexible PO defaults: config header + freeform params + if/then conditions.</summary>
    [DbContext(typeof(RepositoryContext))]
    [Migration("20261124010000_SilaUserPoFlexDefaults")]
    public partial class SilaUserPoFlexDefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sila_user_po_flex_config",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    display_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    config_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    config_version = table.Column<int>(type: "int", nullable: false),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_user_po_flex_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_user_po_flex_config_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_user_po_flex_param",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    config_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    param_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    param_value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_user_po_flex_param", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_user_po_flex_param_config",
                        column: x => x.config_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_user_po_flex_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sila_user_po_flex_condition",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    config_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    when_param = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    when_operator = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    when_value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    then_param = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    then_value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sila_user_po_flex_condition", x => x.id);
                    table.ForeignKey(
                        name: "fk_sila_user_po_flex_condition_config",
                        column: x => x.config_id,
                        principalSchema: "buyersystem",
                        principalTable: "sila_user_po_flex_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sila_user_po_flex_config_is_active",
                schema: "buyersystem",
                table: "sila_user_po_flex_config",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sila_user_po_flex_config_buyer_user_type",
                schema: "buyersystem",
                table: "sila_user_po_flex_config",
                columns: new[] { "buyer_id", "user_id", "config_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_sila_user_po_flex_param_config_id",
                schema: "buyersystem",
                table: "sila_user_po_flex_param",
                column: "config_id");

            migrationBuilder.CreateIndex(
                name: "ix_sila_user_po_flex_condition_config_id",
                schema: "buyersystem",
                table: "sila_user_po_flex_condition",
                column: "config_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sila_user_po_flex_condition", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_user_po_flex_param", schema: "buyersystem");
            migrationBuilder.DropTable(name: "sila_user_po_flex_config", schema: "buyersystem");
        }
    }
}
