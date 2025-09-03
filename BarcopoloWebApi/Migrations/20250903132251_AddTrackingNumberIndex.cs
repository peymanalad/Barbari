using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarcopoloWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingNumberIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_TrackingNumber",
                table: "Orders",
                column: "TrackingNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_TrackingNumber",
                table: "Orders");
        }
    }
}
