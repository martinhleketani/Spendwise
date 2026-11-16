
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpendWise.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogActorDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorEmail",
                table: "AuditLogs",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ActorId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "AuditLogs",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ActorType",
                table: "AuditLogs",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "System")
                .Annotation("MySql:CharSet", "utf8mb4");

            // Preserve the identity of existing administrator activities.
            migrationBuilder.Sql(
                """
                UPDATE AuditLogs
                SET
                    ActorType = 'Administrator',
                    ActorId = AdminUserId,
                    ActorName = AdminName,
                    ActorEmail = AdminEmail
                WHERE AdminUserId IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorEmail",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorType",
                table: "AuditLogs");
        }
    }
}
