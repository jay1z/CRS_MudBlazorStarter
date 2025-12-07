using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantScopedElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CommonElements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "BuildingElements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantElementOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ElementType = table.Column<int>(type: "int", nullable: false),
                    ElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    CustomName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantElementOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantElementOrders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommonElement_Tenant_Active",
                table: "CommonElements",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingElement_Tenant_Active",
                table: "BuildingElements",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantElementOrder_Tenant_Type_Element",
                table: "TenantElementOrders",
                columns: new[] { "TenantId", "ElementType", "ElementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantElementOrder_Tenant_Type_Order",
                table: "TenantElementOrders",
                columns: new[] { "TenantId", "ElementType", "ZOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantElementOrders");

            migrationBuilder.DropIndex(
                name: "IX_CommonElement_Tenant_Active",
                table: "CommonElements");

            migrationBuilder.DropIndex(
                name: "IX_BuildingElement_Tenant_Active",
                table: "BuildingElements");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CommonElements");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BuildingElements");
        }
    }
}
