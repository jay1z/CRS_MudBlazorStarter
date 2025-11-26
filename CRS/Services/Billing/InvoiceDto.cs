namespace CRS.Services.Billing {
    // Lightweight DTO returned by billing APIs and used by UI
    public sealed record InvoiceDto(string Id, long AmountCents, string Currency, System.DateTime CreatedAt, string Status, string? HostedInvoiceUrl);
}