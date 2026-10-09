using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkCustomersToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_UserId",
                table: "customers",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_customers_users_UserId",
                table: "customers",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customers_users_UserId",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_UserId",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "customers");
        }
    }
}
