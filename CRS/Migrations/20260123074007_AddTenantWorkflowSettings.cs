using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantWorkflowSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowAmendmentsAfterCompletion",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AutoArchiveAfterDays",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AutoGenerateInvoiceOnAcceptance",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoRequestFinancialInfo",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoSendInvoiceReminders",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoSendProposalOnApproval",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPaymentTermsDays",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultProposalExpirationDays",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultSiteVisitDurationMinutes",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FinancialInfoDueDays",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOwnerOnStatusChange",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReminderFrequencyDays",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireFinalReview",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireProposalReview",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireServiceContacts",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireSiteVisit",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendAutomaticReminders",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowAmendmentsAfterCompletion",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AutoArchiveAfterDays",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AutoGenerateInvoiceOnAcceptance",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AutoRequestFinancialInfo",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AutoSendInvoiceReminders",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AutoSendProposalOnApproval",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DefaultPaymentTermsDays",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DefaultProposalExpirationDays",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DefaultSiteVisitDurationMinutes",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FinancialInfoDueDays",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "NotifyOwnerOnStatusChange",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ReminderFrequencyDays",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RequireFinalReview",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RequireProposalReview",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RequireServiceContacts",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RequireSiteVisit",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SendAutomaticReminders",
                table: "Tenants");
        }
    }
}
