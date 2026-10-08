using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpendWise.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "RetailerProductPrices",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnPromotion",
                table: "RetailerProductPrices",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PriceSource",
                table: "RetailerProductPrices",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "RegularPrice",
                table: "RetailerProductPrices",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BudgetNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Threshold = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerProductPrices_PriceCheckedAt",
                table: "RetailerProductPrices",
                column: "PriceCheckedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetNotifications_UniqueAlert",
                table: "BudgetNotifications",
                columns: new[] { "UserId", "Category", "Month", "Year", "Threshold" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetNotifications_UserId_CreatedAt",
                table: "BudgetNotifications",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetNotifications");

            migrationBuilder.DropIndex(
                name: "IX_RetailerProductPrices_PriceCheckedAt",
                table: "RetailerProductPrices");

            migrationBuilder.DropColumn(
                name: "IsOnPromotion",
                table: "RetailerProductPrices");

            migrationBuilder.DropColumn(
                name: "PriceSource",
                table: "RetailerProductPrices");

            migrationBuilder.DropColumn(
                name: "RegularPrice",
                table: "RetailerProductPrices");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "RetailerProductPrices",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");
        }
    }
}
