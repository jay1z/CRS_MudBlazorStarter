using System;
using System.ComponentModel.DataAnnotations;

namespace CRS.Models.Billing {
    // Durable log of Stripe webhook events (store subset; raw JSON optional)
    public class StripeEventLog {
        [Key]
        public long Id { get; set; }
        public string EventId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public bool Processed { get; set; }
        public string? SubscriptionId { get; set; }
        public string? CustomerId { get; set; }
        public string? RawJson { get; set; }
        public string? Error { get; set; }
    }
}
