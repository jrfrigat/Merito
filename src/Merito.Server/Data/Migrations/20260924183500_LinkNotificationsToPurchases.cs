using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Merito.Server.Data.Migrations;

/// <inheritdoc />
[DbContext(typeof(MeritoDbContext))]
[Migration("20260924183500_LinkNotificationsToPurchases")]
public partial class LinkNotificationsToPurchases : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PurchaseId",
            table: "Notifications",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_PurchaseId",
            table: "Notifications",
            column: "PurchaseId");

        migrationBuilder.AddForeignKey(
            name: "FK_Notifications_Purchases_PurchaseId",
            table: "Notifications",
            column: "PurchaseId",
            principalTable: "Purchases",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Notifications_Purchases_PurchaseId",
            table: "Notifications");

        migrationBuilder.DropIndex(
            name: "IX_Notifications_PurchaseId",
            table: "Notifications");

        migrationBuilder.DropColumn(
            name: "PurchaseId",
            table: "Notifications");
    }
}
