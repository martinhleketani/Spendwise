using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpendWise.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTransactionIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "WalletTransactions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_WalletId_IdempotencyKey",
                table: "WalletTransactions",
                columns: new[] { "WalletId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_WalletId_IdempotencyKey",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "WalletTransactions");
        }
    }
}
