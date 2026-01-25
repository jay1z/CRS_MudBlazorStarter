using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddMinMaxUsefulLifeAndRemainingLifeYears : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaxUsefulLifeOptionId",
                table: "ReserveStudyCommonElements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MinUsefulLifeOptionId",
                table: "ReserveStudyCommonElements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingLifeYears",
                table: "ReserveStudyCommonElements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaxUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MinUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingLifeYears",
                table: "ReserveStudyBuildingElements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaxUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MinUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingLifeYears",
                table: "ReserveStudyAdditionalElements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_MaxUsefulLifeOptionId",
                table: "ReserveStudyCommonElements",
                column: "MaxUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_MinUsefulLifeOptionId",
                table: "ReserveStudyCommonElements",
                column: "MinUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_MaxUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements",
                column: "MaxUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_MinUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements",
                column: "MinUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_MaxUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements",
                column: "MaxUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_MinUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements",
                column: "MinUsefulLifeOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReserveStudyAdditionalElements_ElementOptions_MaxUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements",
                column: "MaxUsefulLifeOptionId",
                principalTable: "ElementOptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReserveStudyAdditionalElements_ElementOptions_MinUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements",
                column: "MinUsefulLifeOptionId",
                principalTable: "ElementOptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReserveStudyBuildingElements_ElementOptions_MaxUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements",
                column: "MaxUsefulLifeOptionId",
                principalTable: "ElementOptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReserveStudyBuildingElements_ElementOptions_MinUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements",
                column: "MinUsefulLifeOptionId",
                principalTable: "ElementOptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReserveStudyCommonElements_ElementOptions_MaxUsefulLifeOptionId",
                table: "ReserveStudyCommonElements",
                column: "MaxUsefulLifeOptionId",
                principalTable: "ElementOptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReserveStudyCommonElements_ElementOptions_MinUsefulLifeOptionId",
                table: "ReserveStudyCommonElements",
                column: "MinUsefulLifeOptionId",
                principalTable: "ElementOptions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudyAdditionalElements_ElementOptions_MaxUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements");

            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudyAdditionalElements_ElementOptions_MinUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements");

            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudyBuildingElements_ElementOptions_MaxUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements");

            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudyBuildingElements_ElementOptions_MinUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements");

            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudyCommonElements_ElementOptions_MaxUsefulLifeOptionId",
                table: "ReserveStudyCommonElements");

            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudyCommonElements_ElementOptions_MinUsefulLifeOptionId",
                table: "ReserveStudyCommonElements");

            migrationBuilder.DropIndex(
                name: "IX_ReserveStudyCommonElements_MaxUsefulLifeOptionId",
                table: "ReserveStudyCommonElements");

            migrationBuilder.DropIndex(
                name: "IX_ReserveStudyCommonElements_MinUsefulLifeOptionId",
                table: "ReserveStudyCommonElements");

            migrationBuilder.DropIndex(
                name: "IX_ReserveStudyBuildingElements_MaxUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements");

            migrationBuilder.DropIndex(
                name: "IX_ReserveStudyBuildingElements_MinUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements");

            migrationBuilder.DropIndex(
                name: "IX_ReserveStudyAdditionalElements_MaxUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements");

            migrationBuilder.DropIndex(
                name: "IX_ReserveStudyAdditionalElements_MinUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements");

            migrationBuilder.DropColumn(
                name: "MaxUsefulLifeOptionId",
                table: "ReserveStudyCommonElements");

            migrationBuilder.DropColumn(
                name: "MinUsefulLifeOptionId",
                table: "ReserveStudyCommonElements");

            migrationBuilder.DropColumn(
                name: "RemainingLifeYears",
                table: "ReserveStudyCommonElements");

            migrationBuilder.DropColumn(
                name: "MaxUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements");

            migrationBuilder.DropColumn(
                name: "MinUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements");

            migrationBuilder.DropColumn(
                name: "RemainingLifeYears",
                table: "ReserveStudyBuildingElements");

            migrationBuilder.DropColumn(
                name: "MaxUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements");

            migrationBuilder.DropColumn(
                name: "MinUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements");

            migrationBuilder.DropColumn(
                name: "RemainingLifeYears",
                table: "ReserveStudyAdditionalElements");
        }
    }
}
