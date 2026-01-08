using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddProposalsCollectionAndCurrentProposalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proposals_ReserveStudyId",
                table: "Proposals");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentProposalId",
                table: "ReserveStudies",
                type: "uniqueidentifier",
                nullable: true);

            // Migrate existing data: set CurrentProposalId to the existing proposal for each study
            migrationBuilder.Sql(@"
                UPDATE rs
                SET rs.CurrentProposalId = p.Id
                FROM ReserveStudies rs
                INNER JOIN Proposals p ON p.ReserveStudyId = rs.Id
                WHERE rs.CurrentProposalId IS NULL
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_CurrentProposalId",
                table: "ReserveStudies",
                column: "CurrentProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_ReserveStudyId",
                table: "Proposals",
                column: "ReserveStudyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReserveStudies_Proposals_CurrentProposalId",
                table: "ReserveStudies",
                column: "CurrentProposalId",
                principalTable: "Proposals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudies_Proposals_CurrentProposalId",
                table: "ReserveStudies");

            migrationBuilder.DropIndex(
                name: "IX_ReserveStudies_CurrentProposalId",
                table: "ReserveStudies");

            migrationBuilder.DropIndex(
                name: "IX_Proposals_ReserveStudyId",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "CurrentProposalId",
                table: "ReserveStudies");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_ReserveStudyId",
                table: "Proposals",
                column: "ReserveStudyId",
                unique: true);
        }
    }
}
