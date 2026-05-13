using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlNeda.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModelSyncMobileOrdersUsersPharmacies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PharmacyId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationRequestedAt",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientNotes",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "MobileCreatedAt",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Orders",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "admin");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PharmacyId",
                table: "Users",
                column: "PharmacyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Pharmacies_PharmacyId",
                table: "Users",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Pharmacies_PharmacyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PharmacyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PharmacyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancellationRequestedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClientNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MobileCreatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Orders");
        }
    }
}
