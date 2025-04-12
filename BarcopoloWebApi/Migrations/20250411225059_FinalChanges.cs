using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarcopoloWebApi.Migrations
{
    /// <inheritdoc />
    public partial class FinalChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequests_Persons_ReviewedByAdminId",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "DestinationDetails",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedWalletTransactionId",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RequesterNotes",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "SourceWalletOwnerId",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "SourceWalletOwnerType",
                table: "WithdrawalRequests");

            migrationBuilder.RenameColumn(
                name: "ReviewedDate",
                table: "WithdrawalRequests",
                newName: "ReviewedAt");

            migrationBuilder.RenameColumn(
                name: "RequestDate",
                table: "WithdrawalRequests",
                newName: "RequestedAt");

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PerformedByPersonId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Persons_PerformedByPersonId",
                        column: x => x.PerformedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_SourceWalletId",
                table: "WithdrawalRequests",
                column: "SourceWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_SubOrganizations_BranchWalletId",
                table: "SubOrganizations",
                column: "BranchWalletId",
                unique: true,
                filter: "[BranchWalletId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_PersonalWalletId",
                table: "Persons",
                column: "PersonalWalletId",
                unique: true,
                filter: "[PersonalWalletId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OrganizationWalletId",
                table: "Organizations",
                column: "OrganizationWalletId",
                unique: true,
                filter: "[OrganizationWalletId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_OwnerType_OwnerId",
                table: "Wallets",
                columns: new[] { "OwnerType", "OwnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_PerformedByPersonId",
                table: "WalletTransactions",
                column: "PerformedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_WalletId",
                table: "WalletTransactions",
                column: "WalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Wallets_OrganizationWalletId",
                table: "Organizations",
                column: "OrganizationWalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Persons_Wallets_PersonalWalletId",
                table: "Persons",
                column: "PersonalWalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SubOrganizations_Wallets_BranchWalletId",
                table: "SubOrganizations",
                column: "BranchWalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequests_Persons_ReviewedByAdminId",
                table: "WithdrawalRequests",
                column: "ReviewedByAdminId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequests_Wallets_SourceWalletId",
                table: "WithdrawalRequests",
                column: "SourceWalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Wallets_OrganizationWalletId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Persons_Wallets_PersonalWalletId",
                table: "Persons");

            migrationBuilder.DropForeignKey(
                name: "FK_SubOrganizations_Wallets_BranchWalletId",
                table: "SubOrganizations");

            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequests_Persons_ReviewedByAdminId",
                table: "WithdrawalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequests_Wallets_SourceWalletId",
                table: "WithdrawalRequests");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_WithdrawalRequests_SourceWalletId",
                table: "WithdrawalRequests");

            migrationBuilder.DropIndex(
                name: "IX_SubOrganizations_BranchWalletId",
                table: "SubOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_Persons_PersonalWalletId",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_OrganizationWalletId",
                table: "Organizations");

            migrationBuilder.RenameColumn(
                name: "ReviewedAt",
                table: "WithdrawalRequests",
                newName: "ReviewedDate");

            migrationBuilder.RenameColumn(
                name: "RequestedAt",
                table: "WithdrawalRequests",
                newName: "RequestDate");

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "WithdrawalRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationDetails",
                table: "WithdrawalRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "ProcessedWalletTransactionId",
                table: "WithdrawalRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterNotes",
                table: "WithdrawalRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SourceWalletOwnerId",
                table: "WithdrawalRequests",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "SourceWalletOwnerType",
                table: "WithdrawalRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequests_Persons_ReviewedByAdminId",
                table: "WithdrawalRequests",
                column: "ReviewedByAdminId",
                principalTable: "Persons",
                principalColumn: "Id");
        }
    }
}
