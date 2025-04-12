using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarcopoloWebApi.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Persons_PersonId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Addresses_OriginAddressId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_OriginAddressId",
                table: "Organizations");

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
                name: "PersonId1",
                table: "Addresses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SubOrganizationId",
                table: "Addresses",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OriginAddressId1",
                table: "Organizations",
                column: "OriginAddressId1");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_OrganizationId",
                table: "Addresses",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_PersonId1",
                table: "Addresses",
                column: "PersonId1");

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
                name: "FK_Addresses_Persons_PersonId",
                table: "Addresses",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Persons_PersonId1",
                table: "Addresses",
                column: "PersonId1",
                principalTable: "Persons",
                principalColumn: "Id");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Organizations_OrganizationId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Persons_PersonId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Persons_PersonId1",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_SubOrganizations_SubOrganizationId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Addresses_OriginAddressId1",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_OriginAddressId1",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_OrganizationId",
                table: "Addresses");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_PersonId1",
                table: "Addresses");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_SubOrganizationId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "OriginAddressId1",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "PersonId1",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "SubOrganizationId",
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

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OriginAddressId",
                table: "Organizations",
                column: "OriginAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Persons_PersonId",
                table: "Addresses",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Addresses_OriginAddressId",
                table: "Organizations",
                column: "OriginAddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
