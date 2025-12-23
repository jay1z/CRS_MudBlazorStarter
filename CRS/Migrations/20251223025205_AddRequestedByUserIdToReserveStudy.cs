using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestedByUserIdToReserveStudy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByUserId",
                table: "ReserveStudies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_RequestedByUserId",
                table: "ReserveStudies",
                column: "RequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReserveStudies_AspNetUsers_RequestedByUserId",
                table: "ReserveStudies",
                column: "RequestedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudies_AspNetUsers_RequestedByUserId",
                table: "ReserveStudies");

            migrationBuilder.DropIndex(
                name: "IX_ReserveStudies_RequestedByUserId",
                table: "ReserveStudies");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "ReserveStudies");
        }
    }
}
