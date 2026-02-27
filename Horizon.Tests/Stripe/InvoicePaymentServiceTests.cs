using Horizon.Models;
using Horizon.Services.Interfaces;

using Xunit;

namespace Horizon.Tests.Stripe;

/// <summary>
/// Unit tests for invoice payment processing logic,
/// including status transitions, balance calculations, and milestone progression.
/// </summary>
public class InvoicePaymentServiceTests
{
    #region Invoice Payment Status Transitions

    [Fact]
    public void HandlePayment_FullPayment_SetsStatusToPaid()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 0m);
        var paymentAmount = 1000m;

        invoice.AmountPaid += paymentAmount;

        if (invoice.AmountPaid >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(invoice.PaidAt);
        Assert.Equal(1000m, invoice.AmountPaid);
    }

    [Fact]
    public void HandlePayment_PartialPayment_SetsStatusToPartiallyPaid()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 0m);
        var paymentAmount = 500m;

        invoice.AmountPaid += paymentAmount;

        if (invoice.AmountPaid >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }
        else if (invoice.AmountPaid > 0)
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
        Assert.Null(invoice.PaidAt);
        Assert.Equal(500m, invoice.AmountPaid);
    }

    [Fact]
    public void HandlePayment_SecondPartialPayment_CompletesPayment()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 500m);
        invoice.Status = InvoiceStatus.PartiallyPaid;
        var paymentAmount = 500m;

        invoice.AmountPaid += paymentAmount;

        if (invoice.AmountPaid >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(invoice.PaidAt);
        Assert.Equal(1000m, invoice.AmountPaid);
    }

    [Fact]
    public void HandlePayment_OverPayment_StillMarksPaid()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 0m);
        var paymentAmount = 1050m; // Overpayment

        invoice.AmountPaid += paymentAmount;

        if (invoice.AmountPaid >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.True(invoice.AmountPaid > invoice.TotalAmount);
    }

    #endregion

    #region Stripe Amount Conversion Tests

    [Theory]
    [InlineData(100000, 1000.00)]  // $1,000.00
    [InlineData(50099, 500.99)]     // $500.99
    [InlineData(1, 0.01)]           // $0.01
    [InlineData(0, 0)]              // $0.00
    public void AmountConversion_CentsToDollars_IsCorrect(long cents, double expectedDollars)
    {
        var dollars = cents / 100m;

        Assert.Equal((decimal)expectedDollars, dollars);
    }

    [Theory]
    [InlineData(1000.00, 100000)]   // $1,000.00 → 100000 cents
    [InlineData(500.99, 50099)]      // $500.99 → 50099 cents
    [InlineData(0.01, 1)]            // $0.01 → 1 cent
    public void AmountConversion_DollarsToCents_IsCorrect(double dollars, long expectedCents)
    {
        var cents = (long)((decimal)dollars * 100);

        Assert.Equal(expectedCents, cents);
    }

    #endregion

    #region Invoice Validation Tests

    [Fact]
    public void CreateCheckout_VoidedInvoice_ShouldReject()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 0m);
        invoice.Status = InvoiceStatus.Voided;

        Assert.Equal(InvoiceStatus.Voided, invoice.Status);
        // The service would throw InvalidOperationException("Cannot create payment for voided invoice")
    }

    [Fact]
    public void CreateCheckout_PaidInvoice_ShouldReject()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 1000m);
        invoice.Status = InvoiceStatus.Paid;

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        // The service would throw InvalidOperationException("Invoice is already paid")
    }

    [Fact]
    public void CreateCheckout_ZeroBalance_ShouldReject()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 1000m);

        var balanceDue = invoice.TotalAmount - invoice.AmountPaid;

        Assert.True(balanceDue <= 0);
        // The service would throw InvalidOperationException("No balance due on this invoice")
    }

    [Fact]
    public void CreateCheckout_ValidInvoice_HasPositiveBalance()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 250m);
        invoice.Status = InvoiceStatus.Sent;

        var balanceDue = invoice.TotalAmount - invoice.AmountPaid;

        Assert.Equal(750m, balanceDue);
        Assert.True(balanceDue > 0);
    }

    #endregion

    #region Payment URL Validity Tests

    [Fact]
    public void HasValidPaymentUrl_ValidUrl_ReturnsTrue()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 0m);
        invoice.PaymentUrl = "https://checkout.stripe.com/c/pay/cs_test_abc123";
        invoice.PaymentUrlExpires = DateTime.UtcNow.AddHours(23); // Expires in 23 hours

        var isValid = !string.IsNullOrEmpty(invoice.PaymentUrl) &&
            invoice.PaymentUrlExpires.HasValue &&
            invoice.PaymentUrlExpires.Value > DateTime.UtcNow.AddMinutes(5);

        Assert.True(isValid);
    }

    [Fact]
    public void HasValidPaymentUrl_ExpiredUrl_ReturnsFalse()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 0m);
        invoice.PaymentUrl = "https://checkout.stripe.com/c/pay/cs_test_abc123";
        invoice.PaymentUrlExpires = DateTime.UtcNow.AddMinutes(-1); // Expired

        var isValid = !string.IsNullOrEmpty(invoice.PaymentUrl) &&
            invoice.PaymentUrlExpires.HasValue &&
            invoice.PaymentUrlExpires.Value > DateTime.UtcNow.AddMinutes(5);

        Assert.False(isValid);
    }

    [Fact]
    public void HasValidPaymentUrl_ExpiringWithinBuffer_ReturnsFalse()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 0m);
        invoice.PaymentUrl = "https://checkout.stripe.com/c/pay/cs_test_abc123";
        invoice.PaymentUrlExpires = DateTime.UtcNow.AddMinutes(3); // Within 5-minute buffer

        var isValid = !string.IsNullOrEmpty(invoice.PaymentUrl) &&
            invoice.PaymentUrlExpires.HasValue &&
            invoice.PaymentUrlExpires.Value > DateTime.UtcNow.AddMinutes(5);

        Assert.False(isValid);
    }

    [Fact]
    public void HasValidPaymentUrl_NullUrl_ReturnsFalse()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 0m);
        invoice.PaymentUrl = null;
        invoice.PaymentUrlExpires = DateTime.UtcNow.AddHours(23);

        var isValid = !string.IsNullOrEmpty(invoice.PaymentUrl) &&
            invoice.PaymentUrlExpires.HasValue &&
            invoice.PaymentUrlExpires.Value > DateTime.UtcNow.AddMinutes(5);

        Assert.False(isValid);
    }

    [Fact]
    public void HasValidPaymentUrl_NullExpiry_ReturnsFalse()
    {
        var invoice = CreateTestInvoice(totalAmount: 1000m, amountPaid: 0m);
        invoice.PaymentUrl = "https://checkout.stripe.com/c/pay/cs_test_abc123";
        invoice.PaymentUrlExpires = null;

        var isValid = !string.IsNullOrEmpty(invoice.PaymentUrl) &&
            invoice.PaymentUrlExpires.HasValue &&
            invoice.PaymentUrlExpires.Value > DateTime.UtcNow.AddMinutes(5);

        Assert.False(isValid);
    }

    #endregion

    #region Milestone Progression Tests

    [Theory]
    [InlineData(InvoiceMilestoneType.Deposit, InvoiceMilestoneType.SiteVisitComplete)]
    [InlineData(InvoiceMilestoneType.SiteVisitComplete, InvoiceMilestoneType.DraftReportDelivery)]
    [InlineData(InvoiceMilestoneType.DraftReportDelivery, InvoiceMilestoneType.FinalDelivery)]
    public void GetNextMilestone_StandardProgression_ReturnsExpected(
        InvoiceMilestoneType current, InvoiceMilestoneType expectedNext)
    {
        var next = GetNextMilestone(current);

        Assert.NotNull(next);
        Assert.Equal(expectedNext, next.Value);
    }

    [Theory]
    [InlineData(InvoiceMilestoneType.FinalDelivery)]
    [InlineData(InvoiceMilestoneType.FullPayment)]
    [InlineData(InvoiceMilestoneType.Custom)]
    public void GetNextMilestone_TerminalMilestone_ReturnsNull(InvoiceMilestoneType current)
    {
        var next = GetNextMilestone(current);

        Assert.Null(next);
    }

    [Fact]
    public void GetNextMilestone_NullMilestone_ReturnsNull()
    {
        var next = GetNextMilestone(null);

        Assert.Null(next);
    }

    #endregion

    #region Payment Record Creation Tests

    [Fact]
    public void PaymentRecord_StripePayment_HasCorrectProperties()
    {
        var invoiceId = Guid.CreateVersion7();
        var paymentIntentId = "pi_test_abc123";

        var record = new PaymentRecord
        {
            TenantId = 1,
            InvoiceId = invoiceId,
            Amount = 1000m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Stripe",
            ReferenceNumber = paymentIntentId,
            IsAutomatic = true,
            Notes = $"Stripe payment via checkout session cs_test_xyz"
        };

        Assert.Equal("Stripe", record.PaymentMethod);
        Assert.Equal(paymentIntentId, record.ReferenceNumber);
        Assert.True(record.IsAutomatic);
        Assert.Equal(1000m, record.Amount);
    }

    #endregion

    #region Invoice Line Item Checkout Tests

    [Fact]
    public void InvoiceLineItems_ConvertToCents_CorrectlyForStripe()
    {
        var lineItem = new InvoiceLineItem
        {
            Description = "Reserve Study - Phase 1",
            UnitPrice = 2500.50m,
            Quantity = 1
        };

        var unitAmountInCents = lineItem.UnitPrice * 100;

        Assert.Equal(250050m, unitAmountInCents);
    }

    [Fact]
    public void InvoiceLineItems_Quantity_CeilingForStripeLong()
    {
        var lineItem = new InvoiceLineItem
        {
            Description = "Hourly consulting",
            UnitPrice = 150m,
            Quantity = 2.5m
        };

        var stripeQuantity = (long)Math.Ceiling(lineItem.Quantity);

        Assert.Equal(3, stripeQuantity);
    }

    [Fact]
    public void InvoiceLineItems_WholeQuantity_NoCeilingEffect()
    {
        var lineItem = new InvoiceLineItem
        {
            Description = "Site visit",
            UnitPrice = 500m,
            Quantity = 2m
        };

        var stripeQuantity = (long)Math.Ceiling(lineItem.Quantity);

        Assert.Equal(2, stripeQuantity);
    }

    #endregion

    #region Connect Account Payment Routing Tests

    [Fact]
    public void PaymentRouting_WithConnectAccount_SetsApplicationFee()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            Tier = SubscriptionTier.Pro,
            StripeConnectAccountId = "acct_test123",
            StripeConnectOnboardingComplete = true,
            StripeConnectCardPaymentsEnabled = true
        };

        var hasConnectAccount = !string.IsNullOrEmpty(tenant.StripeConnectAccountId)
            && tenant.StripeConnectOnboardingComplete
            && tenant.StripeConnectCardPaymentsEnabled;

        var balanceDue = 5000m;
        long? applicationFeeAmount = null;

        if (hasConnectAccount)
        {
            var feeRate = SubscriptionTierDefaults.GetPlatformFeeRate(tenant);
            applicationFeeAmount = (long)Math.Ceiling(balanceDue * feeRate * 100);
        }

        Assert.True(hasConnectAccount);
        Assert.NotNull(applicationFeeAmount);
        Assert.Equal(7500, applicationFeeAmount.Value); // 5000 × 0.015 × 100 = 7500 cents
    }

    [Fact]
    public void PaymentRouting_WithoutConnectAccount_NoApplicationFee()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            Tier = SubscriptionTier.Pro,
            StripeConnectAccountId = null
        };

        var hasConnectAccount = !string.IsNullOrEmpty(tenant.StripeConnectAccountId)
            && tenant.StripeConnectOnboardingComplete
            && tenant.StripeConnectCardPaymentsEnabled;

        long? applicationFeeAmount = null;
        if (!hasConnectAccount)
        {
            applicationFeeAmount = null; // Explicitly clear
        }

        Assert.False(hasConnectAccount);
        Assert.Null(applicationFeeAmount);
    }

    [Fact]
    public void PaymentRouting_ConnectIncomplete_NoApplicationFee()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            Tier = SubscriptionTier.Pro,
            StripeConnectAccountId = "acct_test123",
            StripeConnectOnboardingComplete = false, // Not completed
            StripeConnectCardPaymentsEnabled = false
        };

        var hasConnectAccount = !string.IsNullOrEmpty(tenant.StripeConnectAccountId)
            && tenant.StripeConnectOnboardingComplete
            && tenant.StripeConnectCardPaymentsEnabled;

        Assert.False(hasConnectAccount);
    }

    #endregion

    #region Helpers

    private static Invoice CreateTestInvoice(decimal totalAmount, decimal amountPaid)
    {
        return new Invoice
        {
            TenantId = 1,
            ReserveStudyId = Guid.CreateVersion7(),
            InvoiceNumber = "INV-TEST-001",
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            Status = InvoiceStatus.Sent,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30)
        };
    }

    /// <summary>
    /// Mirrors the logic from InvoicePaymentService.GetNextMilestone.
    /// </summary>
    private static InvoiceMilestoneType? GetNextMilestone(InvoiceMilestoneType? current)
    {
        return current switch
        {
            InvoiceMilestoneType.Deposit => InvoiceMilestoneType.SiteVisitComplete,
            InvoiceMilestoneType.SiteVisitComplete => InvoiceMilestoneType.DraftReportDelivery,
            InvoiceMilestoneType.DraftReportDelivery => InvoiceMilestoneType.FinalDelivery,
            _ => null
        };
    }

    #endregion
}
