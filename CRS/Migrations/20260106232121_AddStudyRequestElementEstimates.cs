using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyRequestElementEstimates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ElementEstimateNotes",
                table: "StudyRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedAdditionalElementCount",
                table: "StudyRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedBuildingElementCount",
                table: "StudyRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedCommonElementCount",
                table: "StudyRequests",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElementEstimateNotes",
                table: "StudyRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedAdditionalElementCount",
                table: "StudyRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedBuildingElementCount",
                table: "StudyRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedCommonElementCount",
                table: "StudyRequests");
        }
    }
}
