using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddProposalAmendmentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AmendmentNumber",
                table: "Proposals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AmendmentReason",
                table: "Proposals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAmendment",
                table: "Proposals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalProposalId",
                table: "Proposals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_OriginalProposalId",
                table: "Proposals",
                column: "OriginalProposalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proposals_Proposals_OriginalProposalId",
                table: "Proposals",
                column: "OriginalProposalId",
                principalTable: "Proposals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proposals_Proposals_OriginalProposalId",
                table: "Proposals");

            migrationBuilder.DropIndex(
                name: "IX_Proposals_OriginalProposalId",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "AmendmentNumber",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "AmendmentReason",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "IsAmendment",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "OriginalProposalId",
                table: "Proposals");
        }
    }
}
