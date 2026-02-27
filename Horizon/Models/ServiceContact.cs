using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Horizon.Services.Tenant;

namespace Horizon.Models;

/// <summary>
/// Represents a service contact (vendor, contractor, etc.) associated with building/common elements.
/// </summary>
public class ServiceContact : BaseModel, ITenantScoped
{
    // Tenant scope
    public int TenantId { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [DataType(DataType.EmailAddress)]
    [MaxLength(256)]
    public string? Email { get; set; }

    [DataType(DataType.PhoneNumber)]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(10)]
    public string? Extension { get; set; }

    // Service type for categorization
    public ServiceContactType ContactType { get; set; } = ServiceContactType.Other;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";

    public string FullNameInverted => $"{LastName}, {FirstName}";
}

/// <summary>
/// Types of service contacts for categorization.
/// </summary>
public enum ServiceContactType
{
    Other = 0,
    Roofing = 1,
    HVAC = 2,
    Plumbing = 3,
    Electrical = 4,
    Landscaping = 5,
    Paving = 6,
    Painting = 7,
    GeneralContractor = 8,
    PoolService = 9,
    Elevator = 10,
    FireSafety = 11,
    Security = 12
}
