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
                name: "AccessTokens",
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
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
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
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Roles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    EmailNotificationsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WorkflowNotificationsEnabled = table.Column<bool>(type: "bit", nullable: false),
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
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NeedsService = table.Column<bool>(type: "bit", nullable: false),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
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
                name: "CommonElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NeedsService = table.Column<bool>(type: "bit", nullable: false),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
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
                name: "ElementOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionType = table.Column<int>(type: "int", nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinValue = table.Column<int>(type: "int", nullable: true),
                    MaxValue = table.Column<int>(type: "int", nullable: true),
                    ZOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KanbanTasks",
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
                name: "NarrativeInserts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    InsertKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HtmlTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AppliesWhenJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetSectionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InsertAfterBlockKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeInserts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NarrativeTemplateSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PageBreakBefore = table.Column<bool>(type: "bit", nullable: false),
                    CssClass = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeTemplateSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreviewText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HtmlContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlainTextContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TargetPreferences = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SendingStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SendingCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetCount = table.Column<int>(type: "int", nullable: false),
                    SentCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    OpenCount = table.Column<int>(type: "int", nullable: false),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    UnsubscribeCount = table.Column<int>(type: "int", nullable: false),
                    ExternalCampaignId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExternalProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterSubscribers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfirmationToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmationTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsUnsubscribed = table.Column<bool>(type: "bit", nullable: false),
                    UnsubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnsubscribeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Preferences = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExternalProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExternalProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSubscribers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
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
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ContactType = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShowGlobalBanner = table.Column<bool>(type: "bit", nullable: false),
                    GlobalBannerMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GlobalBannerSeverity = table.Column<int>(type: "int", nullable: false),
                    GlobalBannerStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GlobalBannerEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaintenanceModeEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaintenanceMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaintenanceStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaintenanceEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AllowNewTenantSignups = table.Column<bool>(type: "bit", nullable: false),
                    AllowNewUserRegistrations = table.Column<bool>(type: "bit", nullable: false),
                    EnableEmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    EnableSmsNotifications = table.Column<bool>(type: "bit", nullable: false),
                    MaxTenantsAllowed = table.Column<int>(type: "int", nullable: false),
                    MaxUsersPerTenant = table.Column<int>(type: "int", nullable: false),
                    MaxCommunitiesPerTenant = table.Column<int>(type: "int", nullable: false),
                    SupportEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupportPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupportUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SystemVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnnouncementUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentationUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusPageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowAnnouncementBanner = table.Column<bool>(type: "bit", nullable: false),
                    AnnouncementTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnnouncementMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnnouncementStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnnouncementEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DefaultMaxCommunities = table.Column<int>(type: "int", nullable: false),
                    DefaultMaxUsers = table.Column<int>(type: "int", nullable: false),
                    DefaultTrialDays = table.Column<int>(type: "int", nullable: false),
                    RequirePaymentForSignup = table.Column<bool>(type: "bit", nullable: false),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    InactivityWarningMinutes = table.Column<int>(type: "int", nullable: false),
                    RequireReauthenticationForSensitiveActions = table.Column<bool>(type: "bit", nullable: false),
                    EnableApiRateLimiting = table.Column<bool>(type: "bit", nullable: false),
                    ApiRequestsPerMinute = table.Column<int>(type: "int", nullable: false),
                    ApiRequestsPerHour = table.Column<int>(type: "int", nullable: false),
                    EnableAutomatedBackups = table.Column<bool>(type: "bit", nullable: false),
                    BackupIntervalHours = table.Column<int>(type: "int", nullable: false),
                    BackupRetentionDays = table.Column<int>(type: "int", nullable: false),
                    AuditLogRetentionDays = table.Column<int>(type: "int", nullable: false),
                    DeletedDataRetentionDays = table.Column<int>(type: "int", nullable: false),
                    EnableGdprCompliance = table.Column<bool>(type: "bit", nullable: false),
                    AllowDataExport = table.Column<bool>(type: "bit", nullable: false),
                    AllowAccountDeletion = table.Column<bool>(type: "bit", nullable: false),
                    PrivacyPolicyUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsOfServiceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataRetentionYears = table.Column<int>(type: "int", nullable: false),
                    EnableAutomatedReports = table.Column<bool>(type: "bit", nullable: false),
                    DefaultReportFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultReportLogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncludeWatermarkOnReports = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantHomepages",
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
                name: "TenantInvoiceSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    InvoicePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InvoiceNumberFormat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumberPadding = table.Column<int>(type: "int", nullable: false),
                    NextInvoiceNumber = table.Column<int>(type: "int", nullable: false),
                    ResetFrequency = table.Column<int>(type: "int", nullable: false),
                    LastInvoiceYear = table.Column<int>(type: "int", nullable: false),
                    LastInvoiceMonth = table.Column<int>(type: "int", nullable: false),
                    CreditMemoPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NextCreditMemoNumber = table.Column<int>(type: "int", nullable: false),
                    DefaultNetDays = table.Column<int>(type: "int", nullable: false),
                    DefaultEarlyPaymentDiscount = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DefaultEarlyPaymentDays = table.Column<int>(type: "int", nullable: false),
                    DefaultLateInterestRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DefaultGracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    AutoGenerateNextMilestone = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnAutoGenerate = table.Column<bool>(type: "bit", nullable: false),
                    AutoSendOnCreate = table.Column<bool>(type: "bit", nullable: false),
                    NotificationEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EnableAutoReminders = table.Column<bool>(type: "bit", nullable: false),
                    MaxAutoReminders = table.Column<int>(type: "int", nullable: false),
                    FirstReminderDays = table.Column<int>(type: "int", nullable: false),
                    DefaultTaxRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UseTenantBranding = table.Column<bool>(type: "bit", nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Tagline = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FooterText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DefaultTerms = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DefaultNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ShowPaidWatermark = table.Column<bool>(type: "bit", nullable: false),
                    PaymentInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInvoiceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
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
                    LastStripeCheckoutSessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GracePeriodEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletionScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsMarkedForDeletion = table.Column<bool>(type: "bit", nullable: false),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReactivationCount = table.Column<int>(type: "int", nullable: false),
                    LastReactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPaymentFailureAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlatformFeeRateOverride = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    StripeConnectAccountId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StripeConnectOnboardingComplete = table.Column<bool>(type: "bit", nullable: false),
                    StripeConnectPayoutsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    StripeConnectCardPaymentsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    StripeConnectCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StripeConnectLastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultNotificationEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AutoAcceptStudyRequests = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DefaultProposalExpirationDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    AutoSendProposalOnApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequireProposalReview = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SkipProposalStep = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AutoRequestFinancialInfo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FinancialInfoDueDays = table.Column<int>(type: "int", nullable: false, defaultValue: 14),
                    RequireServiceContacts = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SkipFinancialInfoStep = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequireSiteVisit = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DefaultSiteVisitDurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 120),
                    AllowVirtualSiteVisit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SendAutomaticReminders = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReminderFrequencyDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                    NotifyOwnerOnStatusChange = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NotifyClientOnStatusChange = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AutoGenerateInvoiceOnAcceptance = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DefaultPaymentTermsDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    AutoSendInvoiceReminders = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RequireNarrativeReview = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequireFinalReview = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AutoArchiveAfterDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AllowAmendmentsAfterCompletion = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
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
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
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
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogArchives",
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
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogArchives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogArchives_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TableName = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContactGroups",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentTerms = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAccounts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyManagers",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Settings",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NarrativeTemplateBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BlockKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HtmlTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AppliesWhenJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CssClass = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeTemplateBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NarrativeTemplateBlocks_NarrativeTemplateSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "NarrativeTemplateSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DemoSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DemoTenantId = table.Column<int>(type: "int", nullable: true),
                    DemoUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Referrer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConvertedToRealAccount = table.Column<bool>(type: "bit", nullable: false),
                    ConvertedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoSessions_Tenants_DemoTenantId",
                        column: x => x.DemoTenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

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
                name: "UserRoleAssignments",
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContactXContactGroups",
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
                        principalTable: "ContactGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContactXContactGroups_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Communities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnnualMeetingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhysicalAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MailingAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberOfUnits = table.Column<int>(type: "int", nullable: true),
                    YearBuilt = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Communities_Addresses_MailingAddressId",
                        column: x => x.MailingAddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Communities_Addresses_PhysicalAddressId",
                        column: x => x.PhysicalAddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Communities_CustomerAccounts_CustomerAccountId",
                        column: x => x.CustomerAccountId,
                        principalTable: "CustomerAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccountInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccountInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAccountInvitations_CustomerAccounts_CustomerAccountId",
                        column: x => x.CustomerAccountId,
                        principalTable: "CustomerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccountUsers",
                columns: table => new
                {
                    CustomerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccountUsers", x => new { x.CustomerAccountId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CustomerAccountUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerAccountUsers_CustomerAccounts_CustomerAccountId",
                        column: x => x.CustomerAccountId,
                        principalTable: "CustomerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    EventType = table.Column<int>(type: "int", nullable: false),
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
                name: "CreditMemos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreditMemoNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StripeRefundId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRefunded = table.Column<bool>(type: "bit", nullable: false),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BillToName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BillToEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditMemos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditMemos_AspNetUsers_AppliedByUserId",
                        column: x => x.AppliedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CreditMemos_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StorageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Documents_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CcEmails = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    BccEmails = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TemplateType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClickedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BouncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    JanuaryFirstReserveBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DecemberThirtyFirstReserveBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BudgetedContributionLastYear = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BudgetedContributionCurrentYear = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BudgetedContributionNextYear = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OperatingBudgetCurrentYear = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OperatingBudgetNextYear = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalNumberOfUnits = table.Column<int>(type: "int", nullable: true),
                    AnnualMeetingMonth = table.Column<int>(type: "int", nullable: true),
                    AnnualMeetingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoanAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LoanBalanceRemaining = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LoanExpectedYearComplete = table.Column<int>(type: "int", nullable: true),
                    SpecialAssessmentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SpecialAssessmentBalanceRemaining = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SpecialAssessmentExpectedYearComplete = table.Column<int>(type: "int", nullable: true),
                    PlannedProjects = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PropertyInsuranceDeductible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InterestRateOnReserveFunds = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    BuildingRoofSidingInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComponentReplacementDates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SidingCalculationPreference = table.Column<int>(type: "int", nullable: true),
                    AcknowledgementAccepted = table.Column<bool>(type: "bit", nullable: false),
                    CommunityNameOnAcknowledgment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PresidentSignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcknowledgmentSignatureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "GeneratedReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StorageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExcelStorageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    PageCount = table.Column<int>(type: "int", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TemplateUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OutputFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsPublishedToClient = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentToClientAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentToEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SupersedesReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    LastDownloadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedReports_AspNetUsers_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GeneratedReports_AspNetUsers_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GeneratedReports_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GeneratedReports_GeneratedReports_SupersedesReportId",
                        column: x => x.SupersedesReportId,
                        principalTable: "GeneratedReports",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillToName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BillToEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BillToAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    BillToPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EarlyPaymentDiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EarlyPaymentDiscountDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EarlyPaymentDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LatePaymentInterestRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    LateInterestStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LateInterestAccrued = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastInterestCalculationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StripeCheckoutSessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentUrlExpires = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Terms = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MilestoneType = table.Column<int>(type: "int", nullable: true),
                    MilestoneDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MilestonePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    LastReminderSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AccessTokenExpires = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_AspNetUsers_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsAutomatic = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ProposalScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProposalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateReviewed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false),
                    DateApproved = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryTimeframe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentSchedule = table.Column<int>(type: "int", nullable: false),
                    CustomDepositPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PrepaymentDiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PaymentDueDays = table.Column<int>(type: "int", nullable: false),
                    EarlyPaymentDiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EarlyPaymentDiscountDays = table.Column<int>(type: "int", nullable: false),
                    LatePaymentInterestRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    LatePaymentGracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    MinimumDepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsDepositNonRefundable = table.Column<bool>(type: "bit", nullable: false),
                    IncludePrepaymentDiscount = table.Column<bool>(type: "bit", nullable: false),
                    IncludeDigitalDelivery = table.Column<bool>(type: "bit", nullable: false),
                    IncludeComponentInventory = table.Column<bool>(type: "bit", nullable: false),
                    IncludeFundingPlans = table.Column<bool>(type: "bit", nullable: false),
                    IsDeclined = table.Column<bool>(type: "bit", nullable: false),
                    DateDeclined = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeclinedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeclineReasonCategory = table.Column<int>(type: "int", nullable: true),
                    DeclineComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevisionRequested = table.Column<bool>(type: "bit", nullable: false),
                    IsAmendment = table.Column<bool>(type: "bit", nullable: false),
                    OriginalProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AmendmentNumber = table.Column<int>(type: "int", nullable: false),
                    AmendmentReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proposals_Proposals_OriginalProposalId",
                        column: x => x.OriginalProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReserveStudies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SpecialistUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PointOfContactType = table.Column<int>(type: "int", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PropertyManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    DateApproved = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SiteVisitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FiscalYearEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentReserveFunds = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MonthlyReserveContribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AnnualInflationRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AnnualInterestRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StudyType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreparedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudies_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudies_AspNetUsers_SpecialistUserId",
                        column: x => x.SpecialistUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudies_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudies_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReserveStudies_PropertyManagers_PropertyManagerId",
                        column: x => x.PropertyManagerId,
                        principalTable: "PropertyManagers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReserveStudies_Proposals_CurrentProposalId",
                        column: x => x.CurrentProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReserveStudyAdditionalElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    NeedsService = table.Column<bool>(type: "bit", nullable: false),
                    ServiceContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MeasurementOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemainingLifeYears = table.Column<int>(type: "int", nullable: true),
                    RemainingLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MinUsefulLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaxUsefulLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsefulLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplacementCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReserveStudyAdditionalElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ElementOptions_MaxUsefulLifeOptionId",
                        column: x => x.MaxUsefulLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ElementOptions_MeasurementOptionId",
                        column: x => x.MeasurementOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ElementOptions_MinUsefulLifeOptionId",
                        column: x => x.MinUsefulLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ElementOptions_RemainingLifeOptionId",
                        column: x => x.RemainingLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ElementOptions_UsefulLifeOptionId",
                        column: x => x.UsefulLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyAdditionalElements_ServiceContacts_ServiceContactId",
                        column: x => x.ServiceContactId,
                        principalTable: "ServiceContacts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReserveStudyBuildingElements",
                columns: table => new
                {
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    ServiceContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MeasurementOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemainingLifeYears = table.Column<int>(type: "int", nullable: true),
                    RemainingLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MinUsefulLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaxUsefulLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsefulLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplacementCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                        principalTable: "BuildingElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ElementOptions_MaxUsefulLifeOptionId",
                        column: x => x.MaxUsefulLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ElementOptions_MeasurementOptionId",
                        column: x => x.MeasurementOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ElementOptions_MinUsefulLifeOptionId",
                        column: x => x.MinUsefulLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ElementOptions_RemainingLifeOptionId",
                        column: x => x.RemainingLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ElementOptions_UsefulLifeOptionId",
                        column: x => x.UsefulLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudyBuildingElements_ServiceContacts_ServiceContactId",
                        column: x => x.ServiceContactId,
                        principalTable: "ServiceContacts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReserveStudyCommonElements",
                columns: table => new
                {
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommonElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    ServiceContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastServiced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MeasurementOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemainingLifeYears = table.Column<int>(type: "int", nullable: true),
                    RemainingLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MinUsefulLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaxUsefulLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsefulLifeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ElementName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsefulLife = table.Column<int>(type: "int", nullable: true),
                    RemainingLife = table.Column<int>(type: "int", nullable: true),
                    ReplacementCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                        principalTable: "CommonElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ElementOptions_MaxUsefulLifeOptionId",
                        column: x => x.MaxUsefulLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ElementOptions_MeasurementOptionId",
                        column: x => x.MeasurementOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ElementOptions_MinUsefulLifeOptionId",
                        column: x => x.MinUsefulLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ElementOptions_RemainingLifeOptionId",
                        column: x => x.RemainingLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ElementOptions_UsefulLifeOptionId",
                        column: x => x.UsefulLifeOptionId,
                        principalTable: "ElementOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReserveStudyCommonElements_ServiceContacts_ServiceContactId",
                        column: x => x.ServiceContactId,
                        principalTable: "ServiceContacts",
                        principalColumn: "Id");
                });

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
                    OverrideReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AmendmentProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AmendmentAcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AmendmentAcceptedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AmendmentRejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AmendmentRejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeComparisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScopeComparisons_Proposals_AmendmentProposalId",
                        column: x => x.AmendmentProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScopeComparisons_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteVisitPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ElementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StorageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PhotoTakenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    TakenByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IncludeInReport = table.Column<bool>(type: "bit", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteVisitPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteVisitPhotos_AspNetUsers_TakenByUserId",
                        column: x => x.TakenByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteVisitPhotos_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RelatedToStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MentionedUserIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyNotes_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyNotes_AspNetUsers_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudyNotes_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyNotes_StudyNotes_ParentNoteId",
                        column: x => x.ParentNoteId,
                        principalTable: "StudyNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false),
                    EstimatedBuildingElementCount = table.Column<int>(type: "int", nullable: true),
                    EstimatedCommonElementCount = table.Column<int>(type: "int", nullable: true),
                    EstimatedAdditionalElementCount = table.Column<int>(type: "int", nullable: true),
                    ElementEstimateNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StateChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatusChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StatusNotes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    ProposalAccepted = table.Column<bool>(type: "bit", nullable: false),
                    ProposalAcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AmendmentRequired = table.Column<bool>(type: "bit", nullable: false),
                    AmendmentAccepted = table.Column<bool>(type: "bit", nullable: false),
                    AmendmentAcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SiteVisitComplete = table.Column<bool>(type: "bit", nullable: false),
                    SiteVisitCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyRequests_ReserveStudies_Id",
                        column: x => x.Id,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReserveStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTickets_AspNetUsers_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupportTickets_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupportTickets_ReserveStudies_ReserveStudyId",
                        column: x => x.ReserveStudyId,
                        principalTable: "ReserveStudies",
                        principalColumn: "Id");
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

            migrationBuilder.CreateTable(
                name: "StudyStatusHistories",
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
                        principalTable: "StudyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFromStaff = table.Column<bool>(type: "bit", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEdited = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketComments_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketComments_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
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
                name: "IX_AccessTokens_Token",
                table: "AccessTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogArchives_ApplicationUserId",
                table: "AuditLogArchives",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Table_Created_Covering",
                table: "AuditLogs",
                columns: new[] { "TableName", "CreatedAt" })
                .Annotation("SqlServer:Include", new[] { "RecordId", "ColumnName", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ApplicationUserId",
                table: "AuditLogs",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingElement_Tenant_Active",
                table: "BuildingElements",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvent_Tenant",
                table: "CalendarEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvent_Tenant_DateRange",
                table: "CalendarEvents",
                columns: new[] { "TenantId", "Start", "End" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvent_Tenant_Study",
                table: "CalendarEvents",
                columns: new[] { "TenantId", "ReserveStudyId" },
                filter: "[ReserveStudyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvent_Tenant_Type",
                table: "CalendarEvents",
                columns: new[] { "TenantId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_ReserveStudyId",
                table: "CalendarEvents",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_CommonElement_Tenant_Active",
                table: "CommonElements",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Communities_CustomerAccountId",
                table: "Communities",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Communities_MailingAddressId",
                table: "Communities",
                column: "MailingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Communities_PhysicalAddressId",
                table: "Communities",
                column: "PhysicalAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Communities_TenantId",
                table: "Communities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Community_Tenant_Active",
                table: "Communities",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactGroups_ApplicationUserId",
                table: "ContactGroups",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Tenant_User_NotDeleted",
                table: "Contacts",
                columns: new[] { "TenantId", "ApplicationUserId" },
                filter: "[DateDeleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_ApplicationUserId",
                table: "Contacts",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_TenantId",
                table: "Contacts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactXContactGroups_ContactGroupId",
                table: "ContactXContactGroups",
                column: "ContactGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactXContactGroups_ContactId",
                table: "ContactXContactGroups",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_AppliedByUserId",
                table: "CreditMemos",
                column: "AppliedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_CreatedByUserId",
                table: "CreditMemos",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_InvoiceId",
                table: "CreditMemos",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_ReserveStudyId",
                table: "CreditMemos",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountInvitation_Account_Email_Status",
                table: "CustomerAccountInvitations",
                columns: new[] { "CustomerAccountId", "Email", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountInvitation_Status_Expires",
                table: "CustomerAccountInvitations",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountInvitation_Token",
                table: "CustomerAccountInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_TenantId",
                table: "CustomerAccounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_UserId",
                table: "CustomerAccounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountUser_Account_Role_Active",
                table: "CustomerAccountUsers",
                columns: new[] { "CustomerAccountId", "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountUser_User",
                table: "CustomerAccountUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DemoSessions_DemoTenantId",
                table: "DemoSessions",
                column: "DemoTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_Tenant",
                table: "Documents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_Tenant_Community_NotDeleted",
                table: "Documents",
                columns: new[] { "TenantId", "CommunityId" },
                filter: "[DateDeleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Document_Tenant_Study_NotDeleted",
                table: "Documents",
                columns: new[] { "TenantId", "ReserveStudyId" },
                filter: "[DateDeleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Document_Tenant_Type_Public",
                table: "Documents",
                columns: new[] { "TenantId", "Type", "IsPublic" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CommunityId",
                table: "Documents",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ReserveStudyId",
                table: "Documents",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedByUserId",
                table: "Documents",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ElementOption_Type_Active_Order",
                table: "ElementOptions",
                columns: new[] { "OptionType", "IsActive", "ZOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_ExternalMessageId",
                table: "EmailLogs",
                column: "ExternalMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_Tenant",
                table: "EmailLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_Tenant_Email_Sent",
                table: "EmailLogs",
                columns: new[] { "TenantId", "ToEmail", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_Tenant_Status_Sent",
                table: "EmailLogs",
                columns: new[] { "TenantId", "Status", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_ReserveStudyId",
                table: "EmailLogs",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInfo_Tenant_Study_NotDeleted",
                table: "FinancialInfos",
                columns: new[] { "TenantId", "ReserveStudyId" },
                filter: "[DateDeleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInfos_ReserveStudyId",
                table: "FinancialInfos",
                column: "ReserveStudyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInfos_TenantId",
                table: "FinancialInfos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReport_Tenant",
                table: "GeneratedReports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReport_Tenant_Published",
                table: "GeneratedReports",
                columns: new[] { "TenantId", "IsPublishedToClient", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReport_Tenant_Study_Status",
                table: "GeneratedReports",
                columns: new[] { "TenantId", "ReserveStudyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReport_Tenant_Study_Type",
                table: "GeneratedReports",
                columns: new[] { "TenantId", "ReserveStudyId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_GeneratedByUserId",
                table: "GeneratedReports",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_PublishedByUserId",
                table: "GeneratedReports",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_ReserveStudyId",
                table: "GeneratedReports",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_ReviewedByUserId",
                table: "GeneratedReports",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_SupersedesReportId",
                table: "GeneratedReports",
                column: "SupersedesReportId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItem_Tenant",
                table: "InvoiceLineItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItem_Tenant_Invoice_Order",
                table: "InvoiceLineItems",
                columns: new[] { "TenantId", "InvoiceId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_InvoiceId",
                table: "InvoiceLineItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Tenant",
                table: "Invoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Tenant_DueDate_Status",
                table: "Invoices",
                columns: new[] { "TenantId", "DueDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Tenant_Number_Unique",
                table: "Invoices",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true,
                filter: "[DateDeleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Tenant_Status",
                table: "Invoices",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Tenant_Study",
                table: "Invoices",
                columns: new[] { "TenantId", "ReserveStudyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedByUserId",
                table: "Invoices",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ReserveStudyId",
                table: "Invoices",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SentByUserId",
                table: "Invoices",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeInsert_Tenant",
                table: "NarrativeInserts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeInsert_Tenant_Enabled_Section",
                table: "NarrativeInserts",
                columns: new[] { "TenantId", "IsEnabled", "TargetSectionKey" });

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeInsert_Tenant_Key",
                table: "NarrativeInserts",
                columns: new[] { "TenantId", "InsertKey" });

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

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeTemplateBlock_Tenant",
                table: "NarrativeTemplateBlocks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeTemplateBlock_Tenant_Section_Block",
                table: "NarrativeTemplateBlocks",
                columns: new[] { "TenantId", "SectionKey", "BlockKey" });

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeTemplateBlock_Tenant_Section_Enabled_Order",
                table: "NarrativeTemplateBlocks",
                columns: new[] { "TenantId", "SectionKey", "IsEnabled", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeTemplateBlocks_SectionId",
                table: "NarrativeTemplateBlocks",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeTemplateSection_Tenant",
                table: "NarrativeTemplateSections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeTemplateSection_Tenant_Enabled_Order",
                table: "NarrativeTemplateSections",
                columns: new[] { "TenantId", "IsEnabled", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeTemplateSection_Tenant_Key",
                table: "NarrativeTemplateSections",
                columns: new[] { "TenantId", "SectionKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_Tenant",
                table: "Notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_Tenant_User_Created",
                table: "Notifications",
                columns: new[] { "TenantId", "UserId", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_Tenant_User_Read",
                table: "Notifications",
                columns: new[] { "TenantId", "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_InvoiceId",
                table: "PaymentRecords",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_RecordedByUserId",
                table: "PaymentRecords",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_ApplicationUserId",
                table: "Profiles",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyManagers_ApplicationUserId",
                table: "PropertyManagers",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyManagers_TenantId",
                table: "PropertyManagers",
                column: "TenantId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Proposal_Tenant_Study_Approved_NotDeleted",
                table: "Proposals",
                columns: new[] { "TenantId", "ReserveStudyId", "IsApproved" },
                filter: "[DateDeleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_OriginalProposalId",
                table: "Proposals",
                column: "OriginalProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_ReserveStudyId",
                table: "Proposals",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_TenantId",
                table: "Proposals",
                column: "TenantId");

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
                name: "IX_ReserveStudies_ApplicationUserId",
                table: "ReserveStudies",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_CommunityId",
                table: "ReserveStudies",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_ContactId",
                table: "ReserveStudies",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_CurrentProposalId",
                table: "ReserveStudies",
                column: "CurrentProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_PropertyManagerId",
                table: "ReserveStudies",
                column: "PropertyManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_RequestedByUserId",
                table: "ReserveStudies",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_SpecialistUserId",
                table: "ReserveStudies",
                column: "SpecialistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudies_TenantId",
                table: "ReserveStudies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudy_Tenant_Active_Created_Covering",
                table: "ReserveStudies",
                columns: new[] { "TenantId", "IsActive", "DateCreated" })
                .Annotation("SqlServer:Include", new[] { "CommunityId", "ApplicationUserId", "SpecialistUserId", "IsComplete" });

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudy_Tenant_Specialist_Active",
                table: "ReserveStudies",
                columns: new[] { "TenantId", "SpecialistUserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudy_Tenant_Status_Active",
                table: "ReserveStudies",
                columns: new[] { "TenantId", "IsComplete", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudy_Tenant_User_Community",
                table: "ReserveStudies",
                columns: new[] { "TenantId", "ApplicationUserId", "CommunityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_MaxUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements",
                column: "MaxUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_MeasurementOptionId",
                table: "ReserveStudyAdditionalElements",
                column: "MeasurementOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_MinUsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements",
                column: "MinUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_RemainingLifeOptionId",
                table: "ReserveStudyAdditionalElements",
                column: "RemainingLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_ReserveStudyId",
                table: "ReserveStudyAdditionalElements",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_ServiceContactId",
                table: "ReserveStudyAdditionalElements",
                column: "ServiceContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyAdditionalElements_UsefulLifeOptionId",
                table: "ReserveStudyAdditionalElements",
                column: "UsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_BuildingElementId",
                table: "ReserveStudyBuildingElements",
                column: "BuildingElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_MaxUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements",
                column: "MaxUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_MeasurementOptionId",
                table: "ReserveStudyBuildingElements",
                column: "MeasurementOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_MinUsefulLifeOptionId",
                table: "ReserveStudyBuildingElements",
                column: "MinUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_RemainingLifeOptionId",
                table: "ReserveStudyBuildingElements",
                column: "RemainingLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_ServiceContactId",
                table: "ReserveStudyBuildingElements",
                column: "ServiceContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyBuildingElements_UsefulLifeOptionId",
                table: "ReserveStudyBuildingElements",
                column: "UsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_RSBuildingElement_Study_Element",
                table: "ReserveStudyBuildingElements",
                columns: new[] { "ReserveStudyId", "BuildingElementId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_CommonElementId",
                table: "ReserveStudyCommonElements",
                column: "CommonElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_MaxUsefulLifeOptionId",
                table: "ReserveStudyCommonElements",
                column: "MaxUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_MeasurementOptionId",
                table: "ReserveStudyCommonElements",
                column: "MeasurementOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_MinUsefulLifeOptionId",
                table: "ReserveStudyCommonElements",
                column: "MinUsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_RemainingLifeOptionId",
                table: "ReserveStudyCommonElements",
                column: "RemainingLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_ServiceContactId",
                table: "ReserveStudyCommonElements",
                column: "ServiceContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveStudyCommonElements_UsefulLifeOptionId",
                table: "ReserveStudyCommonElements",
                column: "UsefulLifeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_RSCommonElement_Study_Element",
                table: "ReserveStudyCommonElements",
                columns: new[] { "ReserveStudyId", "CommonElementId" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScopeComparison_Tenant_Status",
                table: "ScopeComparisons",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ScopeComparison_Tenant_Study",
                table: "ScopeComparisons",
                columns: new[] { "TenantId", "ReserveStudyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScopeComparisons_AmendmentProposalId",
                table: "ScopeComparisons",
                column: "AmendmentProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeComparisons_ReserveStudyId",
                table: "ScopeComparisons",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceContact_Tenant",
                table: "ServiceContacts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceContact_Tenant_Company_NotDeleted",
                table: "ServiceContacts",
                columns: new[] { "TenantId", "CompanyName" },
                filter: "[DateDeleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceContact_Tenant_Type_Active",
                table: "ServiceContacts",
                columns: new[] { "TenantId", "ContactType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Settings_ApplicationUserId",
                table: "Settings",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisitPhoto_Tenant",
                table: "SiteVisitPhotos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisitPhoto_Tenant_Element",
                table: "SiteVisitPhotos",
                columns: new[] { "TenantId", "ElementId", "ElementType" },
                filter: "[ElementId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisitPhoto_Tenant_Study_Category",
                table: "SiteVisitPhotos",
                columns: new[] { "TenantId", "ReserveStudyId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisitPhoto_Tenant_Study_Order",
                table: "SiteVisitPhotos",
                columns: new[] { "TenantId", "ReserveStudyId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisitPhotos_ReserveStudyId",
                table: "SiteVisitPhotos",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisitPhotos_TakenByUserId",
                table: "SiteVisitPhotos",
                column: "TakenByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNote_Tenant",
                table: "StudyNotes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNote_Tenant_Author",
                table: "StudyNotes",
                columns: new[] { "TenantId", "AuthorUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyNote_Tenant_Study_Pinned",
                table: "StudyNotes",
                columns: new[] { "TenantId", "ReserveStudyId", "IsPinned" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyNote_Tenant_Study_Visibility",
                table: "StudyNotes",
                columns: new[] { "TenantId", "ReserveStudyId", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_AuthorUserId",
                table: "StudyNotes",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_ParentNoteId",
                table: "StudyNotes",
                column: "ParentNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_ReserveStudyId",
                table: "StudyNotes",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_ResolvedByUserId",
                table: "StudyNotes",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyRequests_CommunityId",
                table: "StudyRequests",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyRequests_TenantId",
                table: "StudyRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyRequests_TenantId_CurrentStatus",
                table: "StudyRequests",
                columns: new[] { "TenantId", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyRequests_TenantId_StateChangedAt",
                table: "StudyRequests",
                columns: new[] { "TenantId", "StateChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyStatusHistories_RequestId",
                table: "StudyStatusHistories",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyStatusHistories_TenantId",
                table: "StudyStatusHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyStatusHistories_TenantId_RequestId_ChangedAt",
                table: "StudyStatusHistories",
                columns: new[] { "TenantId", "RequestId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyStatusHistories_TenantId_ToStatus_ChangedAt",
                table: "StudyStatusHistories",
                columns: new[] { "TenantId", "ToStatus", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_Tenant",
                table: "SupportTickets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_Tenant_Assigned_Status",
                table: "SupportTickets",
                columns: new[] { "TenantId", "AssignedToUserId", "Status" },
                filter: "[AssignedToUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_Tenant_Status_Priority",
                table: "SupportTickets",
                columns: new[] { "TenantId", "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_Tenant_Study",
                table: "SupportTickets",
                columns: new[] { "TenantId", "ReserveStudyId" },
                filter: "[ReserveStudyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AssignedToUserId",
                table: "SupportTickets",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedByUserId",
                table: "SupportTickets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_ReserveStudyId",
                table: "SupportTickets",
                column: "ReserveStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantElementOrder_Tenant_Type_Element",
                table: "TenantElementOrders",
                columns: new[] { "TenantId", "ElementType", "ElementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantElementOrder_Tenant_Type_Order",
                table: "TenantElementOrders",
                columns: new[] { "TenantId", "ElementType", "ZOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantHomepages_TenantId",
                table: "TenantHomepages",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantHomepages_TenantId_IsPublished",
                table: "TenantHomepages",
                columns: new[] { "TenantId", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PublicId",
                table: "Tenants",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketComment_Tenant",
                table: "TicketComments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComment_Tenant_Ticket_Posted",
                table: "TicketComments",
                columns: new[] { "TenantId", "TicketId", "PostedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketComment_Tenant_Ticket_Visibility",
                table: "TicketComments",
                columns: new[] { "TenantId", "TicketId", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_AuthorUserId",
                table: "TicketComments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_TicketId",
                table: "TicketComments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_RoleId",
                table: "UserRoleAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_TenantId",
                table: "UserRoleAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserId",
                table: "UserRoleAssignments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_ReserveStudies_ReserveStudyId",
                table: "CalendarEvents",
                column: "ReserveStudyId",
                principalTable: "ReserveStudies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditMemos_Invoices_InvoiceId",
                table: "CreditMemos",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditMemos_ReserveStudies_ReserveStudyId",
                table: "CreditMemos",
                column: "ReserveStudyId",
                principalTable: "ReserveStudies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_ReserveStudies_ReserveStudyId",
                table: "Documents",
                column: "ReserveStudyId",
                principalTable: "ReserveStudies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailLogs_ReserveStudies_ReserveStudyId",
                table: "EmailLogs",
                column: "ReserveStudyId",
                principalTable: "ReserveStudies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialInfos_ReserveStudies_ReserveStudyId",
                table: "FinancialInfos",
                column: "ReserveStudyId",
                principalTable: "ReserveStudies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GeneratedReports_ReserveStudies_ReserveStudyId",
                table: "GeneratedReports",
                column: "ReserveStudyId",
                principalTable: "ReserveStudies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLineItems_Invoices_InvoiceId",
                table: "InvoiceLineItems",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_ReserveStudies_ReserveStudyId",
                table: "Invoices",
                column: "ReserveStudyId",
                principalTable: "ReserveStudies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Narratives_ReserveStudies_ReserveStudyId",
                table: "Narratives",
                column: "ReserveStudyId",
                principalTable: "ReserveStudies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProposalAcceptances_Proposals_ProposalId",
                table: "ProposalAcceptances",
                column: "ProposalId",
                principalTable: "Proposals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProposalAcceptances_ReserveStudies_ReserveStudyId",
                table: "ProposalAcceptances",
                column: "ReserveStudyId",
                principalTable: "ReserveStudies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Proposals_ReserveStudies_ReserveStudyId",
                table: "Proposals",
                column: "ReserveStudyId",
                principalTable: "ReserveStudies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_AspNetUsers_ApplicationUserId",
                table: "Contacts");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerAccounts_AspNetUsers_UserId",
                table: "CustomerAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyManagers_AspNetUsers_ApplicationUserId",
                table: "PropertyManagers");

            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudies_AspNetUsers_ApplicationUserId",
                table: "ReserveStudies");

            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudies_AspNetUsers_RequestedByUserId",
                table: "ReserveStudies");

            migrationBuilder.DropForeignKey(
                name: "FK_ReserveStudies_AspNetUsers_SpecialistUserId",
                table: "ReserveStudies");

            migrationBuilder.DropForeignKey(
                name: "FK_Proposals_ReserveStudies_ReserveStudyId",
                table: "Proposals");

            migrationBuilder.DropTable(
                name: "AccessTokens");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogArchives");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "ContactXContactGroups");

            migrationBuilder.DropTable(
                name: "CreditMemos");

            migrationBuilder.DropTable(
                name: "CustomerAccountInvitations");

            migrationBuilder.DropTable(
                name: "CustomerAccountUsers");

            migrationBuilder.DropTable(
                name: "DemoSessions");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "FinancialInfos");

            migrationBuilder.DropTable(
                name: "GeneratedReports");

            migrationBuilder.DropTable(
                name: "InvoiceLineItems");

            migrationBuilder.DropTable(
                name: "KanbanTasks");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "NarrativeInserts");

            migrationBuilder.DropTable(
                name: "Narratives");

            migrationBuilder.DropTable(
                name: "NarrativeTemplateBlocks");

            migrationBuilder.DropTable(
                name: "NewsletterCampaigns");

            migrationBuilder.DropTable(
                name: "NewsletterSubscribers");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "ProposalAcceptances");

            migrationBuilder.DropTable(
                name: "ReserveScenarioComponents");

            migrationBuilder.DropTable(
                name: "ReserveStudyAdditionalElements");

            migrationBuilder.DropTable(
                name: "ReserveStudyBuildingElements");

            migrationBuilder.DropTable(
                name: "ReserveStudyCommonElements");

            migrationBuilder.DropTable(
                name: "ScopeComparisons");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "SiteVisitPhotos");

            migrationBuilder.DropTable(
                name: "StripeEventLogs");

            migrationBuilder.DropTable(
                name: "StudyNotes");

            migrationBuilder.DropTable(
                name: "StudyStatusHistories");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TenantElementOrders");

            migrationBuilder.DropTable(
                name: "TenantHomepages");

            migrationBuilder.DropTable(
                name: "TenantInvoiceSettings");

            migrationBuilder.DropTable(
                name: "TenantReserveSettings");

            migrationBuilder.DropTable(
                name: "TenantScopeChangeSettings");

            migrationBuilder.DropTable(
                name: "TicketComments");

            migrationBuilder.DropTable(
                name: "UserRoleAssignments");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ContactGroups");

            migrationBuilder.DropTable(
                name: "NarrativeTemplateSections");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "AcceptanceTermsTemplates");

            migrationBuilder.DropTable(
                name: "ReserveStudyScenarios");

            migrationBuilder.DropTable(
                name: "BuildingElements");

            migrationBuilder.DropTable(
                name: "CommonElements");

            migrationBuilder.DropTable(
                name: "ElementOptions");

            migrationBuilder.DropTable(
                name: "ServiceContacts");

            migrationBuilder.DropTable(
                name: "StudyRequests");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ReserveStudies");

            migrationBuilder.DropTable(
                name: "Communities");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "PropertyManagers");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "CustomerAccounts");
        }
    }
}
