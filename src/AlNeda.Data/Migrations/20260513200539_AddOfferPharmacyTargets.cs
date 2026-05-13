using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlNeda.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferPharmacyTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketingOfferPharmacyTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MarketingOfferId = table.Column<int>(type: "INTEGER", nullable: false),
                    PharmacyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingOfferPharmacyTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingOfferPharmacyTargets_MarketingOffers_MarketingOfferId",
                        column: x => x.MarketingOfferId,
                        principalTable: "MarketingOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketingOfferPharmacyTargets_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingOfferPharmacyTargets_MarketingOfferId_PharmacyId",
                table: "MarketingOfferPharmacyTargets",
                columns: new[] { "MarketingOfferId", "PharmacyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingOfferPharmacyTargets_PharmacyId",
                table: "MarketingOfferPharmacyTargets",
                column: "PharmacyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingOfferPharmacyTargets");
        }
    }
}
