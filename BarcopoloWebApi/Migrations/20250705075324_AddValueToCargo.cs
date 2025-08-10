using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarcopoloWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddValueToCargo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Value",
                table: "Cargos",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Value",
                table: "Cargos");
        }
    }
}
