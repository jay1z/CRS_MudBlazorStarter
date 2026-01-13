using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddNarrativeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Narratives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Introduction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PropertyDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Methodology = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FundingAnalysis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalWordCount = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Narratives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Narratives_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Narratives_AspNetUsers_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Narratives_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Narratives_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Narrative_Tenant",
                table: "Narratives",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Narrative_Tenant_Author",
                table: "Narratives",
                columns: new[] { "TenantId", "AuthorUserId" },
                filter: "[AuthorUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Narrative_Tenant_Study",
                table: "Narratives",
                columns: new[] { "TenantId", "ReserveStudyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Narrative_Tenant_Study_Status",
                table: "Narratives",
                columns: new[] { "TenantId", "ReserveStudyId", "Status" },
                filter: "[DateDeleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Narratives_AuthorUserId",
                table: "Narratives",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Narratives_CompletedByUserId",
                table: "Narratives",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Narratives_ReserveStudyId",
                table: "Narratives",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_Narratives_ReviewedByUserId",
                table: "Narratives",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Narratives");
        }
    }
}
