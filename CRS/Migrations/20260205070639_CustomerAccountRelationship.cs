using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class CustomerAccountRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "CustomerAccounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CustomerAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "CustomerAccounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "CustomerAccounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "CustomerAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "CustomerAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CustomerAccounts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "CustomerAccounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "CustomerAccounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "CustomerAccounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "CustomerAccounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "CustomerAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CustomerAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "CustomerAccounts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerAccountId",
                table: "Communities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Communities_CustomerAccountId",
                table: "Communities",
                column: "CustomerAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Communities_CustomerAccounts_CustomerAccountId",
                table: "Communities",
                column: "CustomerAccountId",
                principalTable: "CustomerAccounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Communities_CustomerAccounts_CustomerAccountId",
                table: "Communities");

            migrationBuilder.DropIndex(
                name: "IX_Communities_CustomerAccountId",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "City",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "State",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "CustomerAccountId",
                table: "Communities");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "CustomerAccounts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CustomerAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);
        }
    }
}
