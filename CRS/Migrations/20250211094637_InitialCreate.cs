using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CRS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crs");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Roles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuildingElements",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NeedsService = table.Column<bool>(type: "bit", nullable: false),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingElements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColorClass = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpecialistUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    IsBusy = table.Column<bool>(type: "bit", nullable: false),
                    IsFree = table.Column<bool>(type: "bit", nullable: false),
                    IsTentative = table.Column<bool>(type: "bit", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeclined = table.Column<bool>(type: "bit", nullable: false),
                    IsInvited = table.Column<bool>(type: "bit", nullable: false),
                    IsAttending = table.Column<bool>(type: "bit", nullable: false),
                    IsNotAttending = table.Column<bool>(type: "bit", nullable: false),
                    IsMaybeAttending = table.Column<bool>(type: "bit", nullable: false),
                    IsResponded = table.Column<bool>(type: "bit", nullable: false),
                    IsNotResponded = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    IsFlagged = table.Column<bool>(type: "bit", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    IsMarked = table.Column<bool>(type: "bit", nullable: false),
                    IsStarred = table.Column<bool>(type: "bit", nullable: false),
                    IsLiked = table.Column<bool>(type: "bit", nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsSubscribed = table.Column<bool>(type: "bit", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    IsMuted = table.Column<bool>(type: "bit", nullable: false),
                    IsReported = table.Column<bool>(type: "bit", nullable: false),
                    IsIgnored = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommonElements",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NeedsService = table.Column<bool>(type: "bit", nullable: false),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommonElements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Communities",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElementMeasurementOptions",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementMeasurementOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElementRemainingLifeOptions",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementRemainingLifeOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElementUsefulLifeOptions",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementUsefulLifeOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyManagers",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyManagers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceContacts",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "crs",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "crs",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "crs",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "crs",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "crs",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactGroups",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactGroups_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Class = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settings_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommunityId = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalSchema: "crs",
                        principalTable: "Communities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReserveStudies",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SpecialistUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CommunityId = table.Column<int>(type: "int", nullable: false),
                    PointOfContactType = table.Column<int>(type: "int", nullable: false),
                    ContactId = table.Column<int>(type: "int", nullable: false),
                    PropertyManagerId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    DateApproved = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReserveStudies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReserveStudies_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudies_AspNetUsers_SpecialistUserId",
                        column: x => x.SpecialistUserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudies_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalSchema: "crs",
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudies_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalSchema: "crs",
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudies_PropertyManagers_PropertyManagerId",
                        column: x => x.PropertyManagerId,
                        principalSchema: "crs",
                        principalTable: "PropertyManagers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContactXContactGroups",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactId = table.Column<int>(type: "int", nullable: false),
                    ContactGroupId = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactXContactGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactXContactGroups_ContactGroups_ContactGroupId",
                        column: x => x.ContactGroupId,
                        principalSchema: "crs",
                        principalTable: "ContactGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContactXContactGroups_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalSchema: "crs",
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReserveStudyAdditionalElements",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReserveStudyId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    NeedsService = table.Column<bool>(type: "bit", nullable: false),
                    ServiceContactId = table.Column<int>(type: "int", nullable: true),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ElementMeasurementOptionsId = table.Column<int>(type: "int", nullable: true),
                    ElementRemainingLifeOptionsId = table.Column<int>(type: "int", nullable: true),
                    ElementUsefulLifeOptionsId = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReserveStudyAdditionalElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ElementMeasurementOptions_ElementMeasurementOptionsId",
                        column: x => x.ElementMeasurementOptionsId,
                        principalSchema: "crs",
                        principalTable: "ElementMeasurementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ElementRemainingLifeOptions_ElementRemainingLifeOptionsId",
                        column: x => x.ElementRemainingLifeOptionsId,
                        principalSchema: "crs",
                        principalTable: "ElementRemainingLifeOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ElementUsefulLifeOptions_ElementUsefulLifeOptionsId",
                        column: x => x.ElementUsefulLifeOptionsId,
                        principalSchema: "crs",
                        principalTable: "ElementUsefulLifeOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalSchema: "crs",
                        principalTable: "ReserveStudies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ServiceContacts_ServiceContactId",
                        column: x => x.ServiceContactId,
                        principalSchema: "crs",
                        principalTable: "ServiceContacts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReserveStudyBuildingElements",
                schema: "crs",
                columns: table => new
                {
                    ReserveStudyId = table.Column<int>(type: "int", nullable: false),
                    BuildingElementId = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    ServiceContactId = table.Column<int>(type: "int", nullable: true),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ElementMeasurementOptionsId = table.Column<int>(type: "int", nullable: true),
                    ElementRemainingLifeOptionsId = table.Column<int>(type: "int", nullable: true),
                    ElementUsefulLifeOptionsId = table.Column<int>(type: "int", nullable: true),
                    Id = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReserveStudyBuildingElements", x => new { x.ReserveStudyId, x.BuildingElementId });
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_BuildingElements_BuildingElementId",
                        column: x => x.BuildingElementId,
                        principalSchema: "crs",
                        principalTable: "BuildingElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ElementMeasurementOptions_ElementMeasurementOptionsId",
                        column: x => x.ElementMeasurementOptionsId,
                        principalSchema: "crs",
                        principalTable: "ElementMeasurementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ElementRemainingLifeOptions_ElementRemainingLifeOptionsId",
                        column: x => x.ElementRemainingLifeOptionsId,
                        principalSchema: "crs",
                        principalTable: "ElementRemainingLifeOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ElementUsefulLifeOptions_ElementUsefulLifeOptionsId",
                        column: x => x.ElementUsefulLifeOptionsId,
                        principalSchema: "crs",
                        principalTable: "ElementUsefulLifeOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalSchema: "crs",
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ServiceContacts_ServiceContactId",
                        column: x => x.ServiceContactId,
                        principalSchema: "crs",
                        principalTable: "ServiceContacts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReserveStudyCommonElements",
                schema: "crs",
                columns: table => new
                {
                    ReserveStudyId = table.Column<int>(type: "int", nullable: false),
                    CommonElementId = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    ServiceContactId = table.Column<int>(type: "int", nullable: true),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ElementMeasurementOptionsId = table.Column<int>(type: "int", nullable: true),
                    ElementRemainingLifeOptionsId = table.Column<int>(type: "int", nullable: true),
                    ElementUsefulLifeOptionsId = table.Column<int>(type: "int", nullable: true),
                    Id = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReserveStudyCommonElements", x => new { x.ReserveStudyId, x.CommonElementId });
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_CommonElements_CommonElementId",
                        column: x => x.CommonElementId,
                        principalSchema: "crs",
                        principalTable: "CommonElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ElementMeasurementOptions_ElementMeasurementOptionsId",
                        column: x => x.ElementMeasurementOptionsId,
                        principalSchema: "crs",
                        principalTable: "ElementMeasurementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ElementRemainingLifeOptions_ElementRemainingLifeOptionsId",
                        column: x => x.ElementRemainingLifeOptionsId,
                        principalSchema: "crs",
                        principalTable: "ElementRemainingLifeOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ElementUsefulLifeOptions_ElementUsefulLifeOptionsId",
                        column: x => x.ElementUsefulLifeOptionsId,
                        principalSchema: "crs",
                        principalTable: "ElementUsefulLifeOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalSchema: "crs",
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ServiceContacts_ServiceContactId",
                        column: x => x.ServiceContactId,
                        principalSchema: "crs",
                        principalTable: "ServiceContacts",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "crs",
                table: "BuildingElements",
                columns: new[] { "Id", "DateCreated", "DateDeleted", "DateModified", "IsActive", "LastServiced", "Name", "NeedsService" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Pitched Roof", true },
                    { 2, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Flat Roof", false },
                    { 3, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Siding", true },
                    { 4, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Gutters/Downspouts", false },
                    { 5, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Attached Lighting", false },
                    { 6, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Shutters", false },
                    { 7, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Decks", false },
                    { 8, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Flooring", false },
                    { 9, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Lighting", false },
                    { 10, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Intercom", false },
                    { 11, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Security System", false },
                    { 12, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Elevator(s)", true },
                    { 13, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "AC Unit(s)", false },
                    { 14, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Heating Unit(s)", false },
                    { 15, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Water Unit(s)", false },
                    { 16, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Kitchen", false },
                    { 17, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Bathroom(s)", false },
                    { 18, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Doors", false },
                    { 19, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Windows", false },
                    { 20, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Balconies", false }
                });

            migrationBuilder.InsertData(
                schema: "crs",
                table: "CommonElements",
                columns: new[] { "Id", "DateCreated", "DateDeleted", "DateModified", "IsActive", "LastServiced", "Name", "NeedsService" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Clubhouse", false },
                    { 2, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Pool", false },
                    { 3, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Playground", false },
                    { 4, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Tennis/Ball Courts", false },
                    { 5, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Property Fencing", false },
                    { 6, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Pool(s)/Lake(s)", false },
                    { 7, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Gazebos(s)/Pavilion(s)", false },
                    { 8, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Entry Signage/Structure(s)", false },
                    { 9, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Street Signage", false },
                    { 10, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Roads", true },
                    { 11, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Catch Basins", false },
                    { 12, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Parking", true },
                    { 13, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Sidewalks", true },
                    { 14, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Driveways", true },
                    { 15, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Patios", false },
                    { 16, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Porches", false },
                    { 17, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Privacy Fencing", false },
                    { 18, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Garage(s)", false },
                    { 19, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Pump Station", true },
                    { 20, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Retaining Walls", false },
                    { 21, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Fountains", false },
                    { 22, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Property Lighting", false },
                    { 23, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Street Lighting", false },
                    { 24, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Paved Trails/Paths", false },
                    { 25, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Mail Huts/Boxes", false },
                    { 26, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Fire Hydrants", false },
                    { 27, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Sports Fields", false },
                    { 28, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, true, null, "Shed(s)/Storage", false }
                });

            migrationBuilder.InsertData(
                schema: "crs",
                table: "ElementMeasurementOptions",
                columns: new[] { "Id", "DateCreated", "DateDeleted", "DateModified", "DisplayText", "Unit", "ZOrder" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "Square Feet", "sq. ft.", 0 },
                    { 2, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "Linear Feet", "LF.", 0 },
                    { 3, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "Each", "ea.", 0 }
                });

            migrationBuilder.InsertData(
                schema: "crs",
                table: "ElementRemainingLifeOptions",
                columns: new[] { "Id", "DateCreated", "DateDeleted", "DateModified", "DisplayText", "Unit", "ZOrder" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "1-5 Years", "1-5", 0 },
                    { 2, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "6-10 Years", "6-10", 1 },
                    { 3, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "11-15 Years", "11-15", 2 },
                    { 4, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "16-20 Years", "16-20", 3 },
                    { 5, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "21-25 Years", "21-25", 4 },
                    { 6, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "26-30 Years", "26-30", 5 },
                    { 7, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "31-35 Years", "31-35", 6 },
                    { 8, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "36-40 Years", "36-40", 7 },
                    { 9, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "41-45 Years", "41-45", 8 },
                    { 10, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "46-50 Years", "46-50", 9 },
                    { 11, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "51-55 Years", "51-55", 10 },
                    { 12, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "56-60 Years", "56-60", 11 },
                    { 13, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "61-65 Years", "61-65", 12 },
                    { 14, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "66-70 Years", "66-70", 13 },
                    { 15, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "71-75 Years", "71-75", 14 },
                    { 16, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "76-80 Years", "76-80", 15 }
                });

            migrationBuilder.InsertData(
                schema: "crs",
                table: "ElementUsefulLifeOptions",
                columns: new[] { "Id", "DateCreated", "DateDeleted", "DateModified", "DisplayText", "Unit", "ZOrder" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "1-5 Years", "1-5", 0 },
                    { 2, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "6-10 Years", "6-10", 1 },
                    { 3, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "11-15 Years", "11-15", 2 },
                    { 4, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "16-20 Years", "16-20", 3 },
                    { 5, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "21-25 Years", "21-25", 4 },
                    { 6, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "26-30 Years", "26-30", 5 },
                    { 7, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "31-35 Years", "31-35", 6 },
                    { 8, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "36-40 Years", "36-40", 7 },
                    { 9, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "41-45 Years", "41-45", 8 },
                    { 10, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "46-50 Years", "46-50", 9 },
                    { 11, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "51-55 Years", "51-55", 10 },
                    { 12, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "56-60 Years", "56-60", 11 },
                    { 13, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "61-65 Years", "61-65", 12 },
                    { 14, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "66-70 Years", "66-70", 13 },
                    { 15, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "71-75 Years", "71-75", 14 },
                    { 16, new DateTime(2025, 2, 1, 17, 56, 22, 0, DateTimeKind.Local), null, null, "76-80 Years", "76-80", 15 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CommunityId",
                schema: "crs",
                table: "Addresses",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "crs",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "crs",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "crs",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "crs",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "crs",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "crs",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "crs",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ApplicationUserId",
                schema: "crs",
                table: "AuditLogs",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactGroups_ApplicationUserId",
                schema: "crs",
                table: "ContactGroups",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactXContactGroups_ContactGroupId",
                schema: "crs",
                table: "ContactXContactGroups",
                column: "ContactGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactXContactGroups_ContactId",
                schema: "crs",
                table: "ContactXContactGroups",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_ApplicationUserId",
                schema: "crs",
                table: "Profiles",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_ApplicationUserId",
                schema: "crs",
                table: "ReserveStudies",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_CommunityId",
                schema: "crs",
                table: "ReserveStudies",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_ContactId",
                schema: "crs",
                table: "ReserveStudies",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_PropertyManagerId",
                schema: "crs",
                table: "ReserveStudies",
                column: "PropertyManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_SpecialistUserId",
                schema: "crs",
                table: "ReserveStudies",
                column: "SpecialistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_ElementMeasurementOptionsId",
                schema: "crs",
                table: "ReserveStudyAdditionalElements",
                column: "ElementMeasurementOptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_ElementRemainingLifeOptionsId",
                schema: "crs",
                table: "ReserveStudyAdditionalElements",
                column: "ElementRemainingLifeOptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_ElementUsefulLifeOptionsId",
                schema: "crs",
                table: "ReserveStudyAdditionalElements",
                column: "ElementUsefulLifeOptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_ReserveStudyId",
                schema: "crs",
                table: "ReserveStudyAdditionalElements",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_ServiceContactId",
                schema: "crs",
                table: "ReserveStudyAdditionalElements",
                column: "ServiceContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_BuildingElementId",
                schema: "crs",
                table: "ReserveStudyBuildingElements",
                column: "BuildingElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_ElementMeasurementOptionsId",
                schema: "crs",
                table: "ReserveStudyBuildingElements",
                column: "ElementMeasurementOptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_ElementRemainingLifeOptionsId",
                schema: "crs",
                table: "ReserveStudyBuildingElements",
                column: "ElementRemainingLifeOptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_ElementUsefulLifeOptionsId",
                schema: "crs",
                table: "ReserveStudyBuildingElements",
                column: "ElementUsefulLifeOptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_ServiceContactId",
                schema: "crs",
                table: "ReserveStudyBuildingElements",
                column: "ServiceContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_CommonElementId",
                schema: "crs",
                table: "ReserveStudyCommonElements",
                column: "CommonElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_ElementMeasurementOptionsId",
                schema: "crs",
                table: "ReserveStudyCommonElements",
                column: "ElementMeasurementOptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_ElementRemainingLifeOptionsId",
                schema: "crs",
                table: "ReserveStudyCommonElements",
                column: "ElementRemainingLifeOptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_ElementUsefulLifeOptionsId",
                schema: "crs",
                table: "ReserveStudyCommonElements",
                column: "ElementUsefulLifeOptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_ServiceContactId",
                schema: "crs",
                table: "ReserveStudyCommonElements",
                column: "ServiceContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_ApplicationUserId",
                schema: "crs",
                table: "Settings",
                column: "ApplicationUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "CalendarEvents",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ContactXContactGroups",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Profiles",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ReserveStudyAdditionalElements",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ReserveStudyBuildingElements",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ReserveStudyCommonElements",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Settings",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ContactGroups",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "BuildingElements",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "CommonElements",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ElementMeasurementOptions",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ElementRemainingLifeOptions",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ElementUsefulLifeOptions",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ReserveStudies",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ServiceContacts",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Communities",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Contacts",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "PropertyManagers",
                schema: "crs");
        }
    }
}
