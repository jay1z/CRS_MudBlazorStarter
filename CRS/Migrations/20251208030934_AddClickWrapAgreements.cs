using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddClickWrapAgreements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcceptanceTermsTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TermsText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CheckboxText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AcceptButtonText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PreviousVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptanceTermsTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProposalAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcceptedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypedSignature = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AcceptorTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcceptorOrganization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TermsVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AcceptanceTermsTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermsContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckboxConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    AcceptorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    RevocationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposalAcceptances_AcceptanceTermsTemplates_AcceptanceTermsTemplateId",
                        column: x => x.AcceptanceTermsTemplateId,
                        principalTable: "AcceptanceTermsTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProposalAcceptances_AspNetUsers_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProposalAcceptances_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProposalAcceptances_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceTermsTemplate_Tenant",
                table: "AcceptanceTermsTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceTermsTemplate_Tenant_Active_Effective",
                table: "AcceptanceTermsTemplates",
                columns: new[] { "TenantId", "IsActive", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceTermsTemplate_Tenant_Type_Default",
                table: "AcceptanceTermsTemplates",
                columns: new[] { "TenantId", "Type", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceTermsTemplate_Tenant_Version",
                table: "AcceptanceTermsTemplates",
                columns: new[] { "TenantId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_ProposalAcceptance_Tenant",
                table: "ProposalAcceptances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalAcceptance_Tenant_Study",
                table: "ProposalAcceptances",
                columns: new[] { "TenantId", "ReserveStudyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProposalAcceptance_Tenant_User_Date",
                table: "ProposalAcceptances",
                columns: new[] { "TenantId", "AcceptedByUserId", "AcceptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProposalAcceptance_Tenant_Valid",
                table: "ProposalAcceptances",
                columns: new[] { "TenantId", "IsValid" });

            migrationBuilder.CreateIndex(
                name: "IX_ProposalAcceptances_AcceptanceTermsTemplateId",
                table: "ProposalAcceptances",
                column: "AcceptanceTermsTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalAcceptances_AcceptedByUserId",
                table: "ProposalAcceptances",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalAcceptances_ProposalId",
                table: "ProposalAcceptances",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalAcceptances_ReserveStudyId",
                table: "ProposalAcceptances",
                column: "ReserveStudyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProposalAcceptances");

            migrationBuilder.DropTable(
                name: "AcceptanceTermsTemplates");
        }
    }
}
