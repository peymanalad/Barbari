using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarcopoloWebApi.Migrations
{
    /// <inheritdoc />
    public partial class addRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestinationBankAccount",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Wallets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationBankAccount",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Wallets");
        }
    }
}
