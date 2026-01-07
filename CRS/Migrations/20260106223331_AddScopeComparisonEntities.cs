using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddScopeComparisonEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScopeComparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalBuildingElementCount = table.Column<int>(type: "int", nullable: false),
                    OriginalCommonElementCount = table.Column<int>(type: "int", nullable: false),
                    OriginalAdditionalElementCount = table.Column<int>(type: "int", nullable: false),
                    OriginalCapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualBuildingElementCount = table.Column<int>(type: "int", nullable: false),
                    ActualCommonElementCount = table.Column<int>(type: "int", nullable: false),
                    ActualAdditionalElementCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ComparedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComparedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OverriddenByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OverriddenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OverrideReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeComparisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScopeComparisons_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantScopeChangeSettings",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    VarianceThresholdPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    VarianceThresholdCount = table.Column<int>(type: "int", nullable: false),
                    RequireHoaApproval = table.Column<bool>(type: "bit", nullable: false),
                    UseTwoPhaseProposal = table.Column<bool>(type: "bit", nullable: false),
                    AutoNotifyHoaOnVariance = table.Column<bool>(type: "bit", nullable: false),
                    VarianceNotificationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AllowStaffOverride = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantScopeChangeSettings", x => x.TenantId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScopeComparison_Tenant_Status",
                table: "ScopeComparisons",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ScopeComparison_Tenant_Study",
                table: "ScopeComparisons",
                columns: new[] { "TenantId", "ReserveStudyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScopeComparisons_ReserveStudyId",
                table: "ScopeComparisons",
                column: "ReserveStudyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScopeComparisons");

            migrationBuilder.DropTable(
                name: "TenantScopeChangeSettings");
        }
    }
}
