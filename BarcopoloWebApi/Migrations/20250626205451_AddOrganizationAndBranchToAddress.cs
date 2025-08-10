using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarcopoloWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationAndBranchToAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "PersonId",
                table: "Addresses",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "BranchId",
                table: "Addresses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OrganizationId",
                table: "Addresses",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_BranchId",
                table: "Addresses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_OrganizationId",
                table: "Addresses",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Organizations_OrganizationId",
                table: "Addresses",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_SubOrganizations_BranchId",
                table: "Addresses",
                column: "BranchId",
                principalTable: "SubOrganizations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Organizations_OrganizationId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_SubOrganizations_BranchId",
                table: "Addresses");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_BranchId",
                table: "Addresses");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_OrganizationId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Addresses");

            migrationBuilder.AlterColumn<long>(
                name: "PersonId",
                table: "Addresses",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
