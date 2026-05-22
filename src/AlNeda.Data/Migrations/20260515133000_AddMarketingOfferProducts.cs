using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlNeda.Data.Migrations
{
    [Migration("20260515133000_AddMarketingOfferProducts")]
    public partial class AddMarketingOfferProducts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketingOfferProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MarketingOfferId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    OldPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    NewPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    GiftProduct = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingOfferProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingOfferProducts_MarketingOffers_MarketingOfferId",
                        column: x => x.MarketingOfferId,
                        principalTable: "MarketingOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketingOfferProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingOfferProducts_MarketingOfferId_ProductId",
                table: "MarketingOfferProducts",
                columns: new[] { "MarketingOfferId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingOfferProducts_ProductId",
                table: "MarketingOfferProducts",
                column: "ProductId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MarketingOfferProducts");
        }
    }
}
