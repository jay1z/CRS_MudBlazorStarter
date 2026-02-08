using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeConnectFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeRateOverride",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeConnectAccountId",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StripeConnectCardPaymentsEnabled",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StripeConnectCreatedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StripeConnectLastSyncedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StripeConnectOnboardingComplete",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StripeConnectPayoutsEnabled",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlatformFeeRateOverride",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeConnectAccountId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeConnectCardPaymentsEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeConnectCreatedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeConnectLastSyncedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeConnectOnboardingComplete",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeConnectPayoutsEnabled",
                table: "Tenants");
        }
    }
}
