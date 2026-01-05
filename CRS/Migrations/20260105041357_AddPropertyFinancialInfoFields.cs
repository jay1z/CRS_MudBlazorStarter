using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyFinancialInfoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcknowledgementAccepted",
                table: "FinancialInfos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgmentSignatureDate",
                table: "FinancialInfos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnnualMeetingDate",
                table: "FinancialInfos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnnualMeetingMonth",
                table: "FinancialInfos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetedContributionCurrentYear",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetedContributionLastYear",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetedContributionNextYear",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingRoofSidingInfo",
                table: "FinancialInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunityNameOnAcknowledgment",
                table: "FinancialInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComponentReplacementDates",
                table: "FinancialInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecemberThirtyFirstReserveBalance",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InterestRateOnReserveFunds",
                table: "FinancialInfos",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "JanuaryFirstReserveBalance",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LoanAmount",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LoanBalanceRemaining",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoanExpectedYearComplete",
                table: "FinancialInfos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OperatingBudgetCurrentYear",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OperatingBudgetNextYear",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlannedProjects",
                table: "FinancialInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresidentSignature",
                table: "FinancialInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PropertyInsuranceDeductible",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SidingCalculationPreference",
                table: "FinancialInfos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecialAssessmentAmount",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecialAssessmentBalanceRemaining",
                table: "FinancialInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpecialAssessmentExpectedYearComplete",
                table: "FinancialInfos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalNumberOfUnits",
                table: "FinancialInfos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgementAccepted",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "AcknowledgmentSignatureDate",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "AnnualMeetingDate",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "AnnualMeetingMonth",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "BudgetedContributionCurrentYear",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "BudgetedContributionLastYear",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "BudgetedContributionNextYear",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "BuildingRoofSidingInfo",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "CommunityNameOnAcknowledgment",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "ComponentReplacementDates",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "DecemberThirtyFirstReserveBalance",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "InterestRateOnReserveFunds",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "JanuaryFirstReserveBalance",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "LoanAmount",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "LoanBalanceRemaining",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "LoanExpectedYearComplete",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "OperatingBudgetCurrentYear",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "OperatingBudgetNextYear",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "PlannedProjects",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "PresidentSignature",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "PropertyInsuranceDeductible",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "SidingCalculationPreference",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "SpecialAssessmentAmount",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "SpecialAssessmentBalanceRemaining",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "SpecialAssessmentExpectedYearComplete",
                table: "FinancialInfos");

            migrationBuilder.DropColumn(
                name: "TotalNumberOfUnits",
                table: "FinancialInfos");
        }
    }
}
