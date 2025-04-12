using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarcopoloWebApi.Migrations
{
    /// <inheritdoc />
    public partial class DeleteAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Organizations_OrganizationId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_SubOrganizations_SubOrganizationId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Addresses_OriginAddressId1",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_SubOrganizations_Addresses_OriginAddressId",
                table: "SubOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_SubOrganizations_OriginAddressId",
                table: "SubOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_OriginAddressId1",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_OrganizationId",
                table: "Addresses");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_SubOrganizationId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "OriginAddressId",
                table: "SubOrganizations");

            migrationBuilder.DropColumn(
                name: "OriginAddressId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "OriginAddressId1",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "SubOrganizationId",
                table: "Addresses");

            migrationBuilder.AddColumn<string>(
                name: "OriginAddress",
                table: "SubOrganizations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginAddress",
                table: "Organizations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginAddress",
                table: "SubOrganizations");

            migrationBuilder.DropColumn(
                name: "OriginAddress",
                table: "Organizations");

            migrationBuilder.AddColumn<long>(
                name: "OriginAddressId",
                table: "SubOrganizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OriginAddressId",
                table: "Organizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OriginAddressId1",
                table: "Organizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "PersonId",
                table: "Addresses",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "OrganizationId",
                table: "Addresses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SubOrganizationId",
                table: "Addresses",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubOrganizations_OriginAddressId",
                table: "SubOrganizations",
                column: "OriginAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OriginAddressId1",
                table: "Organizations",
                column: "OriginAddressId1");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_OrganizationId",
                table: "Addresses",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_SubOrganizationId",
                table: "Addresses",
                column: "SubOrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Organizations_OrganizationId",
                table: "Addresses",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_SubOrganizations_SubOrganizationId",
                table: "Addresses",
                column: "SubOrganizationId",
                principalTable: "SubOrganizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Addresses_OriginAddressId1",
                table: "Organizations",
                column: "OriginAddressId1",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubOrganizations_Addresses_OriginAddressId",
                table: "SubOrganizations",
                column: "OriginAddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
