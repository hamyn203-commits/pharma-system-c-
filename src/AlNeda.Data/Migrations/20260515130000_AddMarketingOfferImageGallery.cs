using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlNeda.Data.Migrations
{
    [Migration("20260515130000_AddMarketingOfferImageGallery")]
    public partial class AddMarketingOfferImageGallery : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalImageUrls",
                table: "MarketingOffers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalImageUrls",
                table: "MarketingOffers");
        }
    }
}
