using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

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
                name: "AccessTokens",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Expiration = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Roles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NeedsService = table.Column<bool>(type: "bit", nullable: false),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SpecialistUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: true),
                    End = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NeedsService = table.Column<bool>(type: "bit", nullable: false),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnnualMeetingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccounts",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElementMeasurementOptions",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementUsefulLifeOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KanbanTasks",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssigneeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateSent = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceContacts",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StripeEventLogs",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false),
                    SubscriptionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeEventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantHomepages",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DraftJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DraftHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantHomepages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BrandingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ProvisioningStatus = table.Column<int>(type: "int", nullable: false),
                    ProvisionedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProvisioningError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tier = table.Column<int>(type: "int", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxCommunities = table.Column<int>(type: "int", nullable: false),
                    MaxSpecialistUsers = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionStatus = table.Column<int>(type: "int", nullable: false),
                    SubscriptionActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionCanceledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PendingOwnerEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignupToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastStripeCheckoutSessionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TableName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColumnName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemoteIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContactGroups",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                name: "Contacts",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                name: "PropertyManagers",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyManagers_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Class = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMailingAddress = table.Column<bool>(type: "bit", nullable: false),
                    CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                name: "UserRoleAssignments",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "crs",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "crs",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "crs",
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContactXContactGroups",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                name: "ReserveStudies",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SpecialistUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PointOfContactType = table.Column<int>(type: "int", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PropertyManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DateApproved = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
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
                        principalColumn: "Id");
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
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudies_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalSchema: "crs",
                        principalTable: "Contacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudies_PropertyManagers_PropertyManagerId",
                        column: x => x.PropertyManagerId,
                        principalSchema: "crs",
                        principalTable: "PropertyManagers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinancialInfos",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CurrentReserveFundBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualContribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProjectedAnnualExpenses = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    FinancialDocumentUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateSubmitted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateReviewed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialInfos_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalSchema: "crs",
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ProposalScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProposalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateApproved = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proposals_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalSchema: "crs",
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReserveStudyAdditionalElements",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    NeedsService = table.Column<bool>(type: "bit", nullable: false),
                    ServiceContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ElementMeasurementOptionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ElementRemainingLifeOptionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ElementUsefulLifeOptionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    ServiceContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ElementMeasurementOptionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ElementRemainingLifeOptionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ElementUsefulLifeOptionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommonElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    ServiceContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ElementMeasurementOptionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ElementRemainingLifeOptionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ElementUsefulLifeOptionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "StudyRequests",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StateChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatusChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StatusNotes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyRequests_ReserveStudies_Id",
                        column: x => x.Id,
                        principalSchema: "crs",
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyStatusHistories",
                schema: "crs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyStatusHistories_StudyRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "crs",
                        principalTable: "StudyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessTokens_Token",
                schema: "crs",
                table: "AccessTokens",
                column: "Token",
                unique: true);

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
                name: "IX_AspNetUsers_TenantId",
                schema: "crs",
                table: "AspNetUsers",
                column: "TenantId");

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
                name: "IX_Communities_TenantId",
                schema: "crs",
                table: "Communities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactGroups_ApplicationUserId",
                schema: "crs",
                table: "ContactGroups",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_ApplicationUserId",
                schema: "crs",
                table: "Contacts",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_TenantId",
                schema: "crs",
                table: "Contacts",
                column: "TenantId");

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
                name: "IX_CustomerAccounts_TenantId",
                schema: "crs",
                table: "CustomerAccounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInfos_ReserveStudyId",
                schema: "crs",
                table: "FinancialInfos",
                column: "ReserveStudyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInfos_TenantId",
                schema: "crs",
                table: "FinancialInfos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_ApplicationUserId",
                schema: "crs",
                table: "Profiles",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyManagers_ApplicationUserId",
                schema: "crs",
                table: "PropertyManagers",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyManagers_TenantId",
                schema: "crs",
                table: "PropertyManagers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_ReserveStudyId",
                schema: "crs",
                table: "Proposals",
                column: "ReserveStudyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_TenantId",
                schema: "crs",
                table: "Proposals",
                column: "TenantId");

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
                name: "IX_ReserveStudies_TenantId",
                schema: "crs",
                table: "ReserveStudies",
                column: "TenantId");

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
                name: "IX_Roles_Name",
                schema: "crs",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settings_ApplicationUserId",
                schema: "crs",
                table: "Settings",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyRequests_CommunityId",
                schema: "crs",
                table: "StudyRequests",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyRequests_TenantId",
                schema: "crs",
                table: "StudyRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyRequests_TenantId_CurrentStatus",
                schema: "crs",
                table: "StudyRequests",
                columns: new[] { "TenantId", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyRequests_TenantId_StateChangedAt",
                schema: "crs",
                table: "StudyRequests",
                columns: new[] { "TenantId", "StateChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyStatusHistories_RequestId",
                schema: "crs",
                table: "StudyStatusHistories",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyStatusHistories_TenantId",
                schema: "crs",
                table: "StudyStatusHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyStatusHistories_TenantId_RequestId_ChangedAt",
                schema: "crs",
                table: "StudyStatusHistories",
                columns: new[] { "TenantId", "RequestId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyStatusHistories_TenantId_ToStatus_ChangedAt",
                schema: "crs",
                table: "StudyStatusHistories",
                columns: new[] { "TenantId", "ToStatus", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_TenantId",
                schema: "crs",
                table: "SupportTickets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantHomepages_TenantId",
                schema: "crs",
                table: "TenantHomepages",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantHomepages_TenantId_IsPublished",
                schema: "crs",
                table: "TenantHomepages",
                columns: new[] { "TenantId", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PublicId",
                schema: "crs",
                table: "Tenants",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                schema: "crs",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_RoleId",
                schema: "crs",
                table: "UserRoleAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_TenantId",
                schema: "crs",
                table: "UserRoleAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserId",
                schema: "crs",
                table: "UserRoleAssignments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessTokens",
                schema: "crs");

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
                name: "CustomerAccounts",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "FinancialInfos",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "KanbanTasks",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Profiles",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Proposals",
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
                name: "StripeEventLogs",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "StudyStatusHistories",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "SupportTickets",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "TenantHomepages",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "UserRoleAssignments",
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
                name: "ServiceContacts",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "StudyRequests",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "crs");

            migrationBuilder.DropTable(
                name: "ReserveStudies",
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

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "crs");
        }
    }
}
