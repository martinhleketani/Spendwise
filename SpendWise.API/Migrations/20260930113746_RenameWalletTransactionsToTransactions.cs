using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpendWise.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameWalletTransactionsToTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // RENAME TABLE
            // =====================================================

            migrationBuilder.RenameTable(
                name: "WalletTransactions",
                newName: "Transactions");


            // =====================================================
            // RENAME INDEXES
            // =====================================================

            migrationBuilder.RenameIndex(
                name: "IX_WalletTransactions_UserId",
                table: "Transactions",
                newName: "IX_Transactions_UserId");


            migrationBuilder.RenameIndex(
                name: "IX_WalletTransactions_UserId_RequestId",
                table: "Transactions",
                newName: "IX_Transactions_UserId_RequestId");


            // =====================================================
            // REPLACE FOREIGN KEY WITH NEW NAME
            // =====================================================

            migrationBuilder.DropForeignKey(
                name: "FK_WalletTransactions_Users_UserId",
                table: "Transactions");


            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // RESTORE FOREIGN KEY NAME
            // =====================================================

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions");


            migrationBuilder.AddForeignKey(
                name: "FK_WalletTransactions_Users_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);


            // =====================================================
            // RESTORE INDEX NAMES
            // =====================================================

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                newName: "IX_WalletTransactions_UserId");


            migrationBuilder.RenameIndex(
                name: "IX_Transactions_UserId_RequestId",
                table: "Transactions",
                newName: "IX_WalletTransactions_UserId_RequestId");


            // =====================================================
            // RESTORE TABLE NAME
            // =====================================================

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "WalletTransactions");
        }
    }
}