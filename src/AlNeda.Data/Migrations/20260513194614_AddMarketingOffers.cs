using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlNeda.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceOfferId",
                table: "Orders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarketingOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    OfferType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AudienceRule = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    QuantityLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    RemainingQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                    OldPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NewPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DiscountPercent = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfferEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MarketingOfferId = table.Column<int>(type: "INTEGER", nullable: false),
                    PharmacyId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DeviceKey = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EventDateKey = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    IsUniqueDailyImpression = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsedDeviceFallback = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRejected = table.Column<bool>(type: "INTEGER", nullable: false),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferEvents_MarketingOffers_MarketingOfferId",
                        column: x => x.MarketingOfferId,
                        principalTable: "MarketingOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfferEvents_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SourceOfferId",
                table: "Orders",
                column: "SourceOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingOffers_StartsAt_EndsAt",
                table: "MarketingOffers",
                columns: new[] { "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingOffers_Status",
                table: "MarketingOffers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OfferEvents_MarketingOfferId_EventType_OccurredAt",
                table: "OfferEvents",
                columns: new[] { "MarketingOfferId", "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfferEvents_MarketingOfferId_PharmacyId_DeviceKey_EventDateKey_EventType_IsUniqueDailyImpression",
                table: "OfferEvents",
                columns: new[] { "MarketingOfferId", "PharmacyId", "DeviceKey", "EventDateKey", "EventType", "IsUniqueDailyImpression" },
                unique: true,
                filter: "EventType = 'impression' AND IsRejected = 0 AND IsUniqueDailyImpression = 1");

            migrationBuilder.CreateIndex(
                name: "IX_OfferEvents_PharmacyId",
                table: "OfferEvents",
                column: "PharmacyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_MarketingOffers_SourceOfferId",
                table: "Orders",
                column: "SourceOfferId",
                principalTable: "MarketingOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_MarketingOffers_SourceOfferId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OfferEvents");

            migrationBuilder.DropTable(
                name: "MarketingOffers");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SourceOfferId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SourceOfferId",
                table: "Orders");
        }
    }
}
