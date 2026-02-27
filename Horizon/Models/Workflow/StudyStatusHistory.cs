using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

using Horizon.Services.Tenant;

namespace Horizon.Models.Workflow {
    /// <summary>
    /// Immutable history entry recording a single workflow transition for a StudyRequest.
    /// </summary>
    [Index(nameof(TenantId))]
    [Index(nameof(TenantId), nameof(RequestId), nameof(ChangedAt))]
    [Index(nameof(TenantId), nameof(ToStatus), nameof(ChangedAt))]
    public class StudyStatusHistory : ITenantScoped {
        [Key]
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public int TenantId { get; set; }

        /// <summary>FK to StudyRequest.Id</summary>
        public Guid RequestId { get; set; }

        /// <summary>Status before transition.</summary>
        public StudyStatus FromStatus { get; set; }

        /// <summary>Status after transition.</summary>
        public StudyStatus ToStatus { get; set; }

        /// <summary>When the transition occurred (UTC).</summary>
        [DataType(DataType.DateTime)]
        public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Actor performing the transition (username/user id).</summary>
        [MaxLength(256)]
        public string? ChangedBy { get; set; }

        /// <summary>Optional notes or reason.</summary>
        [MaxLength(1024)]
        public string? Notes { get; set; }

        /// <summary>Origin of the change (UI, API, Scheduler).</summary>
        [MaxLength(64)]
        public string? Source { get; set; }

        /// <summary>Optional correlation id to link with request logs.</summary>
        [MaxLength(64)]
        public string? CorrelationId { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        [ForeignKey(nameof(RequestId))]
        public StudyRequest? Request { get; set; }
    }
}
