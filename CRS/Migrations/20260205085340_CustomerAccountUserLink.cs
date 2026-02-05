using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class CustomerAccountUserLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "CustomerAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_UserId",
                table: "CustomerAccounts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerAccounts_AspNetUsers_UserId",
                table: "CustomerAccounts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerAccounts_AspNetUsers_UserId",
                table: "CustomerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAccounts_UserId",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CustomerAccounts");
        }
    }
}
