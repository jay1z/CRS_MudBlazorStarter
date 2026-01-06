using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class FixReserveCalculatorForeignKeyTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReserveStudyScenarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartYear = table.Column<int>(type: "int", nullable: false),
                    StartingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverrideProjectionYears = table.Column<int>(type: "int", nullable: true),
                    OverrideInflationRate = table.Column<decimal>(type: "decimal(8,6)", nullable: true),
                    OverrideInterestRateAnnual = table.Column<decimal>(type: "decimal(8,6)", nullable: true),
                    OverrideInterestModel = table.Column<int>(type: "int", nullable: true),
                    OverrideContributionStrategy = table.Column<int>(type: "int", nullable: true),
                    OverrideInitialAnnualContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OverrideContributionEscalationRate = table.Column<decimal>(type: "decimal(8,6)", nullable: true),
                    OverrideContributionFrequency = table.Column<int>(type: "int", nullable: true),
                    OverrideContributionTiming = table.Column<int>(type: "int", nullable: true),
                    OverrideExpenditureTiming = table.Column<int>(type: "int", nullable: true),
                    OverrideRoundingPolicy = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReserveStudyScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReserveStudyScenarios_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantReserveSettings",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DefaultProjectionYears = table.Column<int>(type: "int", nullable: false),
                    DefaultInflationRate = table.Column<decimal>(type: "decimal(8,6)", nullable: false),
                    DefaultInterestRateAnnual = table.Column<decimal>(type: "decimal(8,6)", nullable: false),
                    DefaultInterestModel = table.Column<int>(type: "int", nullable: false),
                    DefaultContributionStrategy = table.Column<int>(type: "int", nullable: false),
                    DefaultInitialAnnualContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultContributionEscalationRate = table.Column<decimal>(type: "decimal(8,6)", nullable: false),
                    DefaultContributionFrequency = table.Column<int>(type: "int", nullable: false),
                    DefaultContributionTiming = table.Column<int>(type: "int", nullable: false),
                    DefaultExpenditureTiming = table.Column<int>(type: "int", nullable: false),
                    DefaultRoundingPolicy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantReserveSettings", x => x.TenantId);
                    table.ForeignKey(
                        name: "FK_TenantReserveSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReserveScenarioComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ScenarioId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    CurrentCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InflationRateOverride = table.Column<decimal>(type: "decimal(8,6)", nullable: true),
                    LastServiceYear = table.Column<int>(type: "int", nullable: true),
                    UsefulLifeYears = table.Column<int>(type: "int", nullable: true),
                    RemainingLifeOverrideYears = table.Column<int>(type: "int", nullable: true),
                    CycleYears = table.Column<int>(type: "int", nullable: true),
                    AnnualCostOverride = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LinkedBuildingElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedCommonElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReserveScenarioComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReserveScenarioComponents_BuildingElements_LinkedBuildingElementId",
                        column: x => x.LinkedBuildingElementId,
                        principalTable: "BuildingElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReserveScenarioComponents_CommonElements_LinkedCommonElementId",
                        column: x => x.LinkedCommonElementId,
                        principalTable: "CommonElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReserveScenarioComponents_ReserveStudyScenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "ReserveStudyScenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReserveScenarioComponent_Tenant",
                table: "ReserveScenarioComponents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveScenarioComponent_Tenant_Scenario",
                table: "ReserveScenarioComponents",
                columns: new[] { "TenantId", "ScenarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReserveScenarioComponent_Tenant_Scenario_Category",
                table: "ReserveScenarioComponents",
                columns: new[] { "TenantId", "ScenarioId", "Category" },
                filter: "[DateDeleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveScenarioComponents_LinkedBuildingElementId",
                table: "ReserveScenarioComponents",
                column: "LinkedBuildingElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveScenarioComponents_LinkedCommonElementId",
                table: "ReserveScenarioComponents",
                column: "LinkedCommonElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveScenarioComponents_ScenarioId",
                table: "ReserveScenarioComponents",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyScenario_Tenant",
                table: "ReserveStudyScenarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyScenario_Tenant_Study",
                table: "ReserveStudyScenarios",
                columns: new[] { "TenantId", "ReserveStudyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyScenario_Tenant_Study_Status",
                table: "ReserveStudyScenarios",
                columns: new[] { "TenantId", "ReserveStudyId", "Status" },
                filter: "[DateDeleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyScenarios_ReserveStudyId",
                table: "ReserveStudyScenarios",
                column: "ReserveStudyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReserveScenarioComponents");

            migrationBuilder.DropTable(
                name: "TenantReserveSettings");

            migrationBuilder.DropTable(
                name: "ReserveStudyScenarios");
        }
    }
}
