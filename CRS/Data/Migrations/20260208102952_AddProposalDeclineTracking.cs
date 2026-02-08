using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProposalDeclineTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateDeclined",
                table: "Proposals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeclineComments",
                table: "Proposals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeclineReasonCategory",
                table: "Proposals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeclinedBy",
                table: "Proposals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeclined",
                table: "Proposals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RevisionRequested",
                table: "Proposals",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateDeclined",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "DeclineComments",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "DeclineReasonCategory",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "DeclinedBy",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "IsDeclined",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "RevisionRequested",
                table: "Proposals");
        }
    }
}
