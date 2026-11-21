using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ItoDemandAndReversalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SapMaterialDocument",
                table: "InternalTransferOrder",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalStatus",
                table: "InternalTransferOrder",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                table: "InternalTransferOrder",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversedBy",
                table: "InternalTransferOrder",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedOn",
                table: "InternalTransferOrder",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReversedQty",
                table: "InternalTransferOrderItem",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PendingReverseQty",
                table: "InternalTransferOrderItem",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "WeeklyBucketId",
                table: "InternalTransferOrderItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WeeklyBucketItemId",
                table: "InternalTransferOrderItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WishlistItemId",
                table: "InternalTransferOrderItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RequirementRequestedQty",
                table: "InternalTransferOrderItem",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransferOrderItem_WeeklyBucketItemId",
                table: "InternalTransferOrderItem",
                column: "WeeklyBucketItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransferOrderItem_WishlistItemId",
                table: "InternalTransferOrderItem",
                column: "WishlistItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InternalTransferOrderItem_WeeklyBucketItemId",
                table: "InternalTransferOrderItem");

            migrationBuilder.DropIndex(
                name: "IX_InternalTransferOrderItem_WishlistItemId",
                table: "InternalTransferOrderItem");

            migrationBuilder.DropColumn(name: "SapMaterialDocument", table: "InternalTransferOrder");
            migrationBuilder.DropColumn(name: "ReversalStatus", table: "InternalTransferOrder");
            migrationBuilder.DropColumn(name: "ReversalReason", table: "InternalTransferOrder");
            migrationBuilder.DropColumn(name: "ReversedBy", table: "InternalTransferOrder");
            migrationBuilder.DropColumn(name: "ReversedOn", table: "InternalTransferOrder");
            migrationBuilder.DropColumn(name: "ReversedQty", table: "InternalTransferOrderItem");
            migrationBuilder.DropColumn(name: "PendingReverseQty", table: "InternalTransferOrderItem");
            migrationBuilder.DropColumn(name: "WeeklyBucketId", table: "InternalTransferOrderItem");
            migrationBuilder.DropColumn(name: "WeeklyBucketItemId", table: "InternalTransferOrderItem");
            migrationBuilder.DropColumn(name: "WishlistItemId", table: "InternalTransferOrderItem");
            migrationBuilder.DropColumn(name: "RequirementRequestedQty", table: "InternalTransferOrderItem");
        }
    }
}
