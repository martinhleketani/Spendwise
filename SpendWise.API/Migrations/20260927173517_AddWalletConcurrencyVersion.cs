using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpendWise.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletConcurrencyVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Wallets",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Wallets");
        }
    }
}