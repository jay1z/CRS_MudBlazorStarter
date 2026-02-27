using Coravel.Mailer.Mail.Interfaces;

using Horizon.Data;
using Horizon.Models;
using Horizon.Models.Email;
using Horizon.Models.Emails;
using Horizon.Services.Interfaces;
using Horizon.Services.Tenant;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Horizon.Services;

/// <summary>
/// Service for managing invoices for reserve studies.
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<InvoiceService> _logger;
    private readonly IMailer _mailer;

    public InvoiceService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        ILogger<InvoiceService> logger,
        IMailer mailer)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _logger = logger;
        _mailer = mailer;
    }

    public async Task<IReadOnlyList<Invoice>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems.OrderBy(li => li.SortOrder))
            .Include(i => i.CreatedBy)
            .Include(i => i.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Where(i => i.ReserveStudyId == reserveStudyId && i.DateDeleted == null)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(ct);
    }

    public async Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems.OrderBy(li => li.SortOrder))
            .Include(i => i.CreatedBy)
            .Include(i => i.SentBy)
            .Include(i => i.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Include(i => i.ReserveStudy)
                .ThenInclude(rs => rs!.Contact)
            .Include(i => i.ReserveStudy)
                .ThenInclude(rs => rs!.PropertyManager)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct);
    }

    public async Task<IReadOnlyList<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Invoices
            .AsNoTracking()
            .Include(i => i.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Where(i => i.Status == status && i.DateDeleted == null)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Invoice>> GetOverdueAsync(CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        var today = DateTime.UtcNow.Date;
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Invoices
            .AsNoTracking()
            .Include(i => i.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Where(i => i.DateDeleted == null &&
                       i.DueDate < today &&
                       i.Status != InvoiceStatus.Paid &&
                       i.Status != InvoiceStatus.Voided)
            .OrderBy(i => i.DueDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Invoice>> GetUnpaidAsync(CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Invoices
            .AsNoTracking()
            .Include(i => i.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Where(i => i.DateDeleted == null &&
                       i.Status != InvoiceStatus.Paid &&
                       i.Status != InvoiceStatus.Voided)
            .OrderBy(i => i.DueDate)
            .ToListAsync(ct);
    }

    public async Task<Invoice> CreateAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var study = await context.ReserveStudies
            .Include(rs => rs.Community)
            .Include(rs => rs.Contact)
            .Include(rs => rs.PropertyManager)
            .FirstOrDefaultAsync(rs => rs.Id == reserveStudyId, ct)
            ?? throw new InvalidOperationException($"Reserve study {reserveStudyId} not found");

        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);
        var pointOfContact = study.PointOfContact;

        var invoice = new Invoice
        {
            TenantId = _tenantContext.TenantId.Value,
            ReserveStudyId = reserveStudyId,
            InvoiceNumber = invoiceNumber,
            Status = InvoiceStatus.Draft,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            BillToName = pointOfContact?.FullName ?? study.Community?.Name,
            BillToEmail = pointOfContact?.Email,
            BillToAddress = study.Community?.PhysicalAddress?.FullAddress,
            BillToPhone = pointOfContact?.Phone
        };

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Created invoice {InvoiceNumber} for study {StudyId}", invoiceNumber, reserveStudyId);
        return invoice;
    }

    public async Task<Invoice> CreateFromProposalAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var study = await context.ReserveStudies
            .Include(rs => rs.Community)
            .Include(rs => rs.Contact)
            .Include(rs => rs.PropertyManager)
            .Include(rs => rs.CurrentProposal)
            .FirstOrDefaultAsync(rs => rs.Id == reserveStudyId, ct)
            ?? throw new InvalidOperationException($"Reserve study {reserveStudyId} not found");

        var proposal = study.CurrentProposal;
        if (proposal == null)
            throw new InvalidOperationException("No current proposal found for this study");

        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);
        var pointOfContact = study.PointOfContact;

        var invoice = new Invoice
        {
            TenantId = _tenantContext.TenantId.Value,
            ReserveStudyId = reserveStudyId,
            InvoiceNumber = invoiceNumber,
            Status = InvoiceStatus.Draft,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            BillToName = pointOfContact?.FullName ?? study.Community?.Name,
            BillToEmail = pointOfContact?.Email,
            BillToAddress = study.Community?.PhysicalAddress?.FullAddress,
            BillToPhone = pointOfContact?.Phone,
            Terms = proposal.PaymentTerms
        };

        // Create line item from proposal
        var lineItem = new InvoiceLineItem
        {
            TenantId = _tenantContext.TenantId.Value,
            Description = $"Reserve Study Services - {study.Community?.Name}",
            Quantity = 1,
            UnitPrice = proposal.EstimatedCost,
            LineTotal = proposal.EstimatedCost,
            SortOrder = 0
        };

        invoice.LineItems.Add(lineItem);
        invoice.Subtotal = proposal.EstimatedCost;
        invoice.TotalAmount = proposal.EstimatedCost;

        // Apply prepayment discount if applicable
        if (proposal.IncludePrepaymentDiscount)
        {
            invoice.DiscountAmount = Math.Round(proposal.EstimatedCost * 0.05m, 2); // 5% discount
            invoice.DiscountDescription = "Prepayment Discount (5%)";
            invoice.TotalAmount = invoice.Subtotal - invoice.DiscountAmount;
        }

                context.Invoices.Add(invoice);
                await context.SaveChangesAsync(ct);

                _logger.LogInformation("Created invoice {InvoiceNumber} from proposal for study {StudyId}", invoiceNumber, reserveStudyId);

                // Try to auto-send if AutoSendOnCreate is enabled
                await TryAutoSendInvoiceAsync(invoice, context, null, ct);

                return invoice;
            }

        public async Task<Invoice> CreateFromProposalMilestoneAsync(
            Guid reserveStudyId, 
            InvoiceMilestoneType milestoneType, 
            bool applyPrepaymentDiscount = false,
            CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required");

            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            var study = await context.ReserveStudies
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.CurrentProposal)
                .FirstOrDefaultAsync(rs => rs.Id == reserveStudyId, ct)
                ?? throw new InvalidOperationException($"Reserve study {reserveStudyId} not found");

            var proposal = study.CurrentProposal;
            if (proposal == null)
                throw new InvalidOperationException("No current proposal found for this study");

            // Get the payment milestones
            var milestones = PaymentScheduleCalculator.GetMilestones(
                proposal.PaymentSchedule, 
                proposal.EstimatedCost, 
                proposal.CustomDepositPercentage);

            // Find the requested milestone
            var milestone = milestones.FirstOrDefault(m => m.Type == milestoneType);
            if (milestone == null)
                throw new InvalidOperationException($"Milestone {milestoneType} not found in payment schedule {proposal.PaymentSchedule}");

            // Check if this milestone has already been invoiced
            var existingInvoice = await context.Invoices
                .FirstOrDefaultAsync(i => 
                    i.ReserveStudyId == reserveStudyId && 
                    i.MilestoneType == milestoneType && 
                    i.DateDeleted == null && 
                    i.Status != InvoiceStatus.Voided, ct);

            if (existingInvoice != null)
                throw new InvalidOperationException($"An invoice for milestone '{PaymentScheduleCalculator.GetMilestoneDisplayName(milestoneType)}' already exists");

            var invoiceNumber = await GenerateInvoiceNumberAsync(ct);
            var pointOfContact = study.PointOfContact;
            var invoiceDate = DateTime.UtcNow;

            var invoice = new Invoice
            {
                TenantId = _tenantContext.TenantId.Value,
                ReserveStudyId = reserveStudyId,
                InvoiceNumber = invoiceNumber,
                Status = InvoiceStatus.Draft,
                InvoiceDate = invoiceDate,
                DueDate = invoiceDate.AddDays(proposal.PaymentDueDays),
                BillToName = pointOfContact?.FullName ?? study.Community?.Name,
                BillToEmail = pointOfContact?.Email,
                BillToAddress = study.Community?.PhysicalAddress?.FullAddress,
                BillToPhone = pointOfContact?.Phone,
                Terms = PaymentScheduleCalculator.FormatPaymentTerms(
                    proposal.PaymentDueDays,
                    proposal.EarlyPaymentDiscountPercentage,
                    proposal.EarlyPaymentDiscountDays,
                    proposal.LatePaymentInterestRate),
                MilestoneType = milestoneType,
                MilestoneDescription = milestone.Description,
                MilestonePercentage = milestone.Percentage,
                // Early payment discount terms
                EarlyPaymentDiscountPercentage = proposal.EarlyPaymentDiscountPercentage,
                EarlyPaymentDiscountDate = proposal.EarlyPaymentDiscountPercentage > 0 
                    ? PaymentScheduleCalculator.GetEarlyPaymentDiscountDate(invoiceDate, proposal.EarlyPaymentDiscountDays)
                    : null,
                // Late payment interest terms
                LatePaymentInterestRate = proposal.LatePaymentInterestRate,
                LateInterestStartDate = proposal.LatePaymentInterestRate > 0
                    ? PaymentScheduleCalculator.GetLateInterestStartDate(
                        invoiceDate.AddDays(proposal.PaymentDueDays), 
                        proposal.LatePaymentGracePeriodDays)
                    : null
            };

            // Create line item from milestone
            var lineItem = new InvoiceLineItem
            {
                TenantId = _tenantContext.TenantId.Value,
                Description = $"Reserve Study Services - {study.Community?.Name}\n{milestone.Description}",
                Quantity = 1,
                UnitPrice = milestone.Amount,
                LineTotal = milestone.Amount,
                SortOrder = 0
            };

            invoice.LineItems.Add(lineItem);
            invoice.Subtotal = milestone.Amount;
            invoice.TotalAmount = milestone.Amount;

            // Apply prepayment discount if requested and applicable
            if (applyPrepaymentDiscount && proposal.IncludePrepaymentDiscount && proposal.PrepaymentDiscountPercentage > 0)
            {
                invoice.DiscountAmount = PaymentScheduleCalculator.CalculatePrepaymentDiscount(
                    milestone.Amount, 
                    proposal.PrepaymentDiscountPercentage);
                invoice.DiscountDescription = $"Prepayment Discount ({proposal.PrepaymentDiscountPercentage}%)";
                invoice.TotalAmount = invoice.Subtotal - invoice.DiscountAmount;
            }

            // Calculate early payment discount amount (for display purposes)
            if (invoice.EarlyPaymentDiscountPercentage > 0)
            {
                invoice.EarlyPaymentDiscountAmount = PaymentScheduleCalculator.CalculateEarlyPaymentDiscount(
                    invoice.TotalAmount,
                    invoice.EarlyPaymentDiscountPercentage);
            }

                context.Invoices.Add(invoice);
                await context.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Created invoice {InvoiceNumber} for milestone {Milestone} ({Percentage}%) on study {StudyId}", 
                    invoiceNumber, milestoneType, milestone.Percentage, reserveStudyId);

                // Try to auto-send if AutoSendOnCreate is enabled
                await TryAutoSendInvoiceAsync(invoice, context, null, ct);

                return invoice;
            }

        public async Task<List<PaymentMilestone>> GetPaymentMilestonesAsync(Guid reserveStudyId, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue) return [];

            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            var study = await context.ReserveStudies
                .Include(rs => rs.CurrentProposal)
                .FirstOrDefaultAsync(rs => rs.Id == reserveStudyId, ct);

            if (study?.CurrentProposal == null) return [];

            var proposal = study.CurrentProposal;
            var milestones = PaymentScheduleCalculator.GetMilestones(
                proposal.PaymentSchedule, 
                proposal.EstimatedCost, 
                proposal.CustomDepositPercentage);

            // Get existing invoices for this study
            var invoices = await context.Invoices
                .Where(i => i.ReserveStudyId == reserveStudyId && 
                           i.DateDeleted == null && 
                           i.Status != InvoiceStatus.Voided)
                .ToListAsync(ct);

            // Mark which milestones have been invoiced/paid
            foreach (var milestone in milestones)
            {
                var matchingInvoice = invoices.FirstOrDefault(i => i.MilestoneType == milestone.Type);
                if (matchingInvoice != null)
                {
                    milestone.IsInvoiced = true;
                    milestone.IsPaid = matchingInvoice.Status == InvoiceStatus.Paid;
                    milestone.InvoiceId = matchingInvoice.Id;
                }
            }

            return milestones;
        }

        public async Task<decimal> GetInvoicedTotalAsync(Guid reserveStudyId, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue) return 0;

            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            return await context.Invoices
                .Where(i => i.ReserveStudyId == reserveStudyId && 
                           i.DateDeleted == null && 
                           i.Status != InvoiceStatus.Voided &&
                           i.Status != InvoiceStatus.Draft)
                .SumAsync(i => i.TotalAmount, ct);
        }

        public async Task<decimal> GetPaidTotalAsync(Guid reserveStudyId, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue) return 0;

            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            return await context.Invoices
                .Where(i => i.ReserveStudyId == reserveStudyId && 
                           i.DateDeleted == null && 
                           i.Status != InvoiceStatus.Voided)
                .SumAsync(i => i.AmountPaid, ct);
        }

        public async Task<Invoice> UpdateAsync(Invoice invoice, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var existing = await context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoice.Id} not found");

        if (existing.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be updated");

        existing.BillToName = invoice.BillToName;
        existing.BillToEmail = invoice.BillToEmail;
        existing.BillToAddress = invoice.BillToAddress;
        existing.BillToPhone = invoice.BillToPhone;
        existing.DueDate = invoice.DueDate;
        existing.TaxRate = invoice.TaxRate;
        existing.DiscountAmount = invoice.DiscountAmount;
        existing.DiscountDescription = invoice.DiscountDescription;
        existing.Notes = invoice.Notes;
        existing.InternalNotes = invoice.InternalNotes;
        existing.Terms = invoice.Terms;
        existing.DateModified = DateTime.UtcNow;

        existing.RecalculateTotals();

        await context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<InvoiceLineItem> AddLineItemAsync(Guid invoiceId, InvoiceLineItem lineItem, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var invoice = await context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Line items can only be added to draft invoices");

        lineItem.TenantId = _tenantContext.TenantId.Value;
        lineItem.InvoiceId = invoiceId;
        lineItem.SortOrder = invoice.LineItems.Count;
        lineItem.RecalculateTotal();

        context.InvoiceLineItems.Add(lineItem);
        invoice.LineItems.Add(lineItem);
        invoice.RecalculateTotals();
        invoice.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return lineItem;
    }

    public async Task<InvoiceLineItem> UpdateLineItemAsync(InvoiceLineItem lineItem, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var existing = await context.InvoiceLineItems
            .Include(li => li.Invoice)
                .ThenInclude(i => i!.LineItems)
            .FirstOrDefaultAsync(li => li.Id == lineItem.Id && li.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Line item {lineItem.Id} not found");

        if (existing.Invoice?.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Line items can only be updated on draft invoices");

        existing.Description = lineItem.Description;
        existing.Quantity = lineItem.Quantity;
        existing.Unit = lineItem.Unit;
        existing.UnitPrice = lineItem.UnitPrice;
        existing.SortOrder = lineItem.SortOrder;
        existing.RecalculateTotal();
        existing.DateModified = DateTime.UtcNow;

        existing.Invoice!.RecalculateTotals();
        existing.Invoice.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task RemoveLineItemAsync(Guid lineItemId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var lineItem = await context.InvoiceLineItems
            .Include(li => li.Invoice)
                .ThenInclude(i => i!.LineItems)
            .FirstOrDefaultAsync(li => li.Id == lineItemId && li.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Line item {lineItemId} not found");

        if (lineItem.Invoice?.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Line items can only be removed from draft invoices");

        lineItem.DateDeleted = DateTime.UtcNow;
        lineItem.Invoice.LineItems.Remove(lineItem);
        lineItem.Invoice.RecalculateTotals();
        lineItem.Invoice.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
    }

    public async Task<Invoice> FinalizeAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var invoice = await context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be finalized");

        if (!invoice.LineItems.Any())
            throw new InvalidOperationException("Cannot finalize an invoice with no line items");

        invoice.Status = InvoiceStatus.Finalized;
        invoice.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        _logger.LogInformation("Finalized invoice {InvoiceNumber}", invoice.InvoiceNumber);
        return invoice;
    }

    public async Task<Invoice> MarkSentAsync(Guid invoiceId, Guid sentByUserId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        if (invoice.Status != InvoiceStatus.Finalized && invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only finalized or draft invoices can be marked as sent");

        invoice.Status = InvoiceStatus.Sent;
        invoice.SentAt = DateTime.UtcNow;
        invoice.SentByUserId = sentByUserId;
        invoice.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        _logger.LogInformation("Marked invoice {InvoiceNumber} as sent", invoice.InvoiceNumber);
        return invoice;
    }

    public async Task<Invoice> MarkViewedAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        if (invoice.Status == InvoiceStatus.Sent)
        {
            invoice.Status = InvoiceStatus.Viewed;
            invoice.DateModified = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Marked invoice {InvoiceNumber} as viewed", invoice.InvoiceNumber);
        }

        return invoice;
    }

    public async Task<Invoice> RecordPaymentAsync(Guid invoiceId, decimal amount, string? paymentMethod = null, string? paymentReference = null, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive", nameof(amount));

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already fully paid");

        if (invoice.Status == InvoiceStatus.Voided)
            throw new InvalidOperationException("Cannot record payment on a voided invoice");

        // Create payment record for audit trail
        var paymentRecord = new PaymentRecord
        {
            TenantId = _tenantContext.TenantId.Value,
            InvoiceId = invoiceId,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = paymentMethod,
            ReferenceNumber = paymentReference,
            IsAutomatic = false
        };
        context.PaymentRecords.Add(paymentRecord);

        invoice.AmountPaid += amount;
        invoice.PaymentMethod = paymentMethod ?? invoice.PaymentMethod;
        invoice.PaymentReference = paymentReference ?? invoice.PaymentReference;
        invoice.DateModified = DateTime.UtcNow;

        if (invoice.AmountPaid >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
            _logger.LogInformation("Invoice {InvoiceNumber} fully paid", invoice.InvoiceNumber);
        }
        else
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
            _logger.LogInformation("Invoice {InvoiceNumber} partially paid: {AmountPaid} of {Total}", 
                invoice.InvoiceNumber, invoice.AmountPaid, invoice.TotalAmount);
        }

        await context.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<Invoice> VoidAsync(Guid invoiceId, string? reason = null, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot void a paid invoice");

        if (invoice.Status == InvoiceStatus.Voided)
            throw new InvalidOperationException("Invoice is already voided");

        invoice.Status = InvoiceStatus.Voided;
        invoice.VoidedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(reason))
        {
            invoice.InternalNotes = string.IsNullOrEmpty(invoice.InternalNotes) 
                ? $"Voided: {reason}" 
                : $"{invoice.InternalNotes}\n\nVoided: {reason}";
            }
            invoice.DateModified = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Voided invoice {InvoiceNumber}", invoice.InvoiceNumber);
            return invoice;
        }

        public async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required");

            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            // Get or create tenant invoice settings
            var settings = await GetOrCreateInvoiceSettingsAsync(context, ct);

            var now = DateTime.UtcNow;
            var year = now.Year;
            var month = now.Month;

            // Check if we need to reset the sequence
            var shouldReset = settings.ResetFrequency switch
            {
                NumberResetFrequency.Yearly => settings.LastInvoiceYear != year,
                NumberResetFrequency.Monthly => settings.LastInvoiceYear != year || settings.LastInvoiceMonth != month,
                _ => false
            };

            if (shouldReset)
            {
                settings.NextInvoiceNumber = 1;
                settings.LastInvoiceYear = year;
                settings.LastInvoiceMonth = month;
            }

            // Generate the invoice number using the format pattern
            var invoiceNumber = settings.InvoiceNumberFormat
                .Replace("{PREFIX}", settings.InvoicePrefix)
                .Replace("{YEAR}", year.ToString())
                .Replace("{MONTH}", month.ToString("D2"))
                .Replace("{NUMBER}", settings.NextInvoiceNumber.ToString($"D{settings.NumberPadding}"));

            // Increment the next number
            settings.NextInvoiceNumber++;
            settings.DateModified = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            return invoiceNumber;
        }

        /// <summary>
        /// Gets or creates the tenant invoice settings.
        /// </summary>
        public async Task<TenantInvoiceSettings> GetOrCreateInvoiceSettingsAsync(CancellationToken ct = default)
        {
            await using var context = await _dbFactory.CreateDbContextAsync(ct);
            return await GetOrCreateInvoiceSettingsAsync(context, ct);
        }

        private async Task<TenantInvoiceSettings> GetOrCreateInvoiceSettingsAsync(ApplicationDbContext context, CancellationToken ct)
        {
            if (!_tenantContext.TenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required");

            var settings = await context.TenantInvoiceSettings
                .FirstOrDefaultAsync(s => s.TenantId == _tenantContext.TenantId.Value && s.DateDeleted == null, ct);

            if (settings == null)
            {
                settings = new TenantInvoiceSettings
                {
                    TenantId = _tenantContext.TenantId.Value
                };
                context.TenantInvoiceSettings.Add(settings);
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Created default invoice settings for tenant {TenantId}", _tenantContext.TenantId.Value);
            }

            return settings;
        }

        /// <summary>
        /// Updates the tenant invoice settings.
        /// </summary>
        public async Task<TenantInvoiceSettings> UpdateInvoiceSettingsAsync(TenantInvoiceSettings settings, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required");

            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            var existingSettings = await context.TenantInvoiceSettings
                .FirstOrDefaultAsync(s => s.Id == settings.Id && s.TenantId == _tenantContext.TenantId.Value, ct)
                ?? throw new InvalidOperationException("Settings not found");

            // Update fields
            existingSettings.InvoicePrefix = settings.InvoicePrefix;
            existingSettings.InvoiceNumberFormat = settings.InvoiceNumberFormat;
            existingSettings.NumberPadding = settings.NumberPadding;
            existingSettings.ResetFrequency = settings.ResetFrequency;
            existingSettings.CreditMemoPrefix = settings.CreditMemoPrefix;
            existingSettings.DefaultNetDays = settings.DefaultNetDays;
                existingSettings.DefaultEarlyPaymentDiscount = settings.DefaultEarlyPaymentDiscount;
                existingSettings.DefaultEarlyPaymentDays = settings.DefaultEarlyPaymentDays;
                existingSettings.DefaultLateInterestRate = settings.DefaultLateInterestRate;
                existingSettings.DefaultGracePeriodDays = settings.DefaultGracePeriodDays;
                existingSettings.AutoGenerateNextMilestone = settings.AutoGenerateNextMilestone;
                existingSettings.NotifyOnAutoGenerate = settings.NotifyOnAutoGenerate;
                existingSettings.AutoSendOnCreate = settings.AutoSendOnCreate;
                existingSettings.NotificationEmail = settings.NotificationEmail;
                existingSettings.EnableAutoReminders = settings.EnableAutoReminders;
                existingSettings.MaxAutoReminders = settings.MaxAutoReminders;
                existingSettings.FirstReminderDays = settings.FirstReminderDays;
                existingSettings.DefaultTaxRate = settings.DefaultTaxRate;
                existingSettings.TaxLabel = settings.TaxLabel;
                // Branding fields
                existingSettings.UseTenantBranding = settings.UseTenantBranding;
                existingSettings.PrimaryColor = settings.PrimaryColor;
                existingSettings.SecondaryColor = settings.SecondaryColor;
                existingSettings.LogoUrl = settings.LogoUrl;
                existingSettings.CompanyName = settings.CompanyName;
                existingSettings.CompanyAddress = settings.CompanyAddress;
                existingSettings.CompanyPhone = settings.CompanyPhone;
                existingSettings.CompanyEmail = settings.CompanyEmail;
                existingSettings.CompanyWebsite = settings.CompanyWebsite;
                existingSettings.Tagline = settings.Tagline;
                existingSettings.FooterText = settings.FooterText;
                existingSettings.DefaultTerms = settings.DefaultTerms;
                existingSettings.DefaultNotes = settings.DefaultNotes;
                existingSettings.ShowPaidWatermark = settings.ShowPaidWatermark;
                existingSettings.PaymentInstructions = settings.PaymentInstructions;
                existingSettings.DateModified = DateTime.UtcNow;

                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Updated invoice settings for tenant {TenantId}", _tenantContext.TenantId.Value);

                return existingSettings;
            }

            /// <summary>
            /// Generates a preview of what the next invoice number will look like.
            /// </summary>
            public async Task<string> PreviewInvoiceNumberAsync(TenantInvoiceSettings settings, CancellationToken ct = default)
            {
                var now = DateTime.UtcNow;
                var year = now.Year;
                var month = now.Month;

                var nextNumber = settings.NextInvoiceNumber;

                // Check if would reset
                var shouldReset = settings.ResetFrequency switch
                {
                    NumberResetFrequency.Yearly => settings.LastInvoiceYear != year,
                    NumberResetFrequency.Monthly => settings.LastInvoiceYear != year || settings.LastInvoiceMonth != month,
                    _ => false
                };

                if (shouldReset)
                {
                    nextNumber = 1;
                }

                return settings.InvoiceNumberFormat
                .Replace("{PREFIX}", settings.InvoicePrefix)
                .Replace("{YEAR}", year.ToString())
                .Replace("{MONTH}", month.ToString("D2"))
                .Replace("{NUMBER}", nextNumber.ToString($"D{settings.NumberPadding}"));
        }

        public async Task UpdateOverdueStatusesAsync(CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue) return;

            var today = DateTime.UtcNow.Date;
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var overdueInvoices = await context.Invoices
            .Where(i => i.DateDeleted == null &&
                       i.DueDate < today &&
                       (i.Status == InvoiceStatus.Sent || 
                        i.Status == InvoiceStatus.Viewed || 
                        i.Status == InvoiceStatus.PartiallyPaid))
            .ToListAsync(ct);

        foreach (var invoice in overdueInvoices)
        {
            invoice.Status = InvoiceStatus.Overdue;
            invoice.DateModified = DateTime.UtcNow;
        }

        if (overdueInvoices.Count > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Marked {Count} invoices as overdue", overdueInvoices.Count);
        }
    }

        public async Task DeleteAsync(Guid invoiceId, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required");

            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            var invoice = await context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
                ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

            if (invoice.Status != InvoiceStatus.Draft)
                throw new InvalidOperationException("Only draft invoices can be deleted");

            invoice.DateDeleted = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Deleted invoice {InvoiceNumber}", invoice.InvoiceNumber);
        }

        public async Task<int> CalculateLateInterestAsync(CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue) return 0;

            var today = DateTime.UtcNow.Date;
            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            // Get all invoices with late interest configured that are overdue
            var invoicesWithInterest = await context.Invoices
                .Where(i => i.DateDeleted == null &&
                           i.LatePaymentInterestRate > 0 &&
                           i.LateInterestStartDate.HasValue &&
                           i.LateInterestStartDate.Value.Date <= today &&
                           i.Status != InvoiceStatus.Paid &&
                           i.Status != InvoiceStatus.Voided)
                .ToListAsync(ct);

            var updatedCount = 0;
            foreach (var invoice in invoicesWithInterest)
            {
                var lastCalcDate = invoice.LastInterestCalculationDate?.Date ?? invoice.LateInterestStartDate!.Value.Date;
                var daysToCalculate = (today - lastCalcDate).Days;

                if (daysToCalculate > 0)
                {
                    var unpaidBalance = invoice.TotalAmount - invoice.AmountPaid;
                    if (unpaidBalance > 0)
                    {
                        var newInterest = PaymentScheduleCalculator.CalculateLateInterest(
                            unpaidBalance,
                            invoice.LatePaymentInterestRate,
                            daysToCalculate);

                        invoice.LateInterestAccrued += newInterest;
                        invoice.LastInterestCalculationDate = today;
                        invoice.DateModified = DateTime.UtcNow;
                        updatedCount++;

                        _logger.LogInformation(
                            "Added {Interest:C} late interest to invoice {InvoiceNumber} ({Days} days at {Rate}%)",
                            newInterest, invoice.InvoiceNumber, daysToCalculate, invoice.LatePaymentInterestRate);
                    }
                }
            }

            if (updatedCount > 0)
            {
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Calculated late interest for {Count} invoices", updatedCount);
            }

            return updatedCount;
        }

        public async Task<decimal> CalculateLateInterestForInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue) return 0;

            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            var invoice = await context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct);

            if (invoice == null || 
                invoice.LatePaymentInterestRate <= 0 || 
                !invoice.LateInterestStartDate.HasValue)
                return 0;

            var today = DateTime.UtcNow.Date;
            if (today <= invoice.LateInterestStartDate.Value.Date)
                return 0;

            var daysOverdue = (today - invoice.LateInterestStartDate.Value.Date).Days;
            var unpaidBalance = invoice.TotalAmount - invoice.AmountPaid;

            return PaymentScheduleCalculator.CalculateLateInterest(
                unpaidBalance,
                invoice.LatePaymentInterestRate,
                daysOverdue);
        }

        public async Task<Invoice> ApplyEarlyPaymentDiscountAsync(Guid invoiceId, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required");

            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            var invoice = await context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
                ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

            if (!invoice.IsEarlyPaymentDiscountAvailable)
                throw new InvalidOperationException("Early payment discount is not available for this invoice");

            // Apply the early payment discount as an additional discount
            var discountDescription = string.IsNullOrEmpty(invoice.DiscountDescription)
                ? $"Early Payment Discount ({invoice.EarlyPaymentDiscountPercentage}%)"
                : $"{invoice.DiscountDescription} + Early Payment ({invoice.EarlyPaymentDiscountPercentage}%)";

            invoice.DiscountAmount += invoice.EarlyPaymentDiscountAmount;
            invoice.DiscountDescription = discountDescription;
            invoice.TotalAmount = invoice.Subtotal + invoice.TaxAmount - invoice.DiscountAmount;
            invoice.DateModified = DateTime.UtcNow;

            // Clear the early payment fields since discount has been applied
            invoice.EarlyPaymentDiscountPercentage = 0;
            invoice.EarlyPaymentDiscountAmount = 0;
            invoice.EarlyPaymentDiscountDate = null;

                    await context.SaveChangesAsync(ct);
                        _logger.LogInformation("Applied early payment discount to invoice {InvoiceNumber}", invoice.InvoiceNumber);

                        return invoice;
                    }

                public async Task<Invoice> DuplicateAsync(Guid invoiceId, CancellationToken ct = default)
                {
                    if (!_tenantContext.TenantId.HasValue)
                        throw new InvalidOperationException("Tenant context is required");

                    await using var context = await _dbFactory.CreateDbContextAsync(ct);

                    var original = await context.Invoices
                        .Include(i => i.LineItems.Where(li => li.DateDeleted == null))
                        .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
                        ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

                    var newInvoiceNumber = await GenerateInvoiceNumberAsync(ct);

                    var duplicate = new Invoice
                    {
                        TenantId = _tenantContext.TenantId.Value,
                        ReserveStudyId = original.ReserveStudyId,
                        InvoiceNumber = newInvoiceNumber,
                        Status = InvoiceStatus.Draft,
                        InvoiceDate = DateTime.UtcNow,
                        DueDate = DateTime.UtcNow.AddDays(30),
                        BillToName = original.BillToName,
                        BillToEmail = original.BillToEmail,
                        BillToAddress = original.BillToAddress,
                        BillToPhone = original.BillToPhone,
                        Subtotal = original.Subtotal,
                        TaxRate = original.TaxRate,
                        TaxAmount = original.TaxAmount,
                        DiscountAmount = original.DiscountAmount,
                        DiscountDescription = original.DiscountDescription,
                        TotalAmount = original.TotalAmount,
                        Terms = original.Terms,
                        Notes = original.Notes,
                        MilestoneType = original.MilestoneType,
                        MilestoneDescription = original.MilestoneDescription,
                        MilestonePercentage = original.MilestonePercentage,
                        // Copy payment terms
                        EarlyPaymentDiscountPercentage = original.EarlyPaymentDiscountPercentage,
                        LatePaymentInterestRate = original.LatePaymentInterestRate
                    };

                    // Calculate new early payment and late interest dates if applicable
                    if (duplicate.EarlyPaymentDiscountPercentage > 0)
                    {
                        duplicate.EarlyPaymentDiscountDate = duplicate.InvoiceDate.AddDays(10); // Default 10 days
                        duplicate.EarlyPaymentDiscountAmount = PaymentScheduleCalculator.CalculateEarlyPaymentDiscount(
                            duplicate.TotalAmount, duplicate.EarlyPaymentDiscountPercentage);
                    }

                    if (duplicate.LatePaymentInterestRate > 0)
                    {
                        duplicate.LateInterestStartDate = duplicate.DueDate;
                    }

                                        // Copy line items
                                        foreach (var lineItem in original.LineItems)
                                        {
                                            duplicate.LineItems.Add(new InvoiceLineItem
                                            {
                                                TenantId = _tenantContext.TenantId.Value,
                                                Description = lineItem.Description,
                                                Quantity = lineItem.Quantity,
                                                UnitPrice = lineItem.UnitPrice,
                                                LineTotal = lineItem.LineTotal,
                                                SortOrder = lineItem.SortOrder
                                            });
                                        }

                                        context.Invoices.Add(duplicate);
                                        await context.SaveChangesAsync(ct);

                                        _logger.LogInformation(
                                            "Duplicated invoice {OriginalNumber} to {NewNumber}",
                                            original.InvoiceNumber, duplicate.InvoiceNumber);

                                        return duplicate;
                                    }

                        public async Task<Invoice> RecordReminderSentAsync(Guid invoiceId, CancellationToken ct = default)
                        {
                            if (!_tenantContext.TenantId.HasValue)
                                throw new InvalidOperationException("Tenant context is required");

                            await using var context = await _dbFactory.CreateDbContextAsync(ct);

                            var invoice = await context.Invoices
                                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
                                ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

                            invoice.ReminderCount++;
                                                        invoice.LastReminderSent = DateTime.UtcNow;
                                                        invoice.DateModified = DateTime.UtcNow;

                                                        await context.SaveChangesAsync(ct);
                                                        _logger.LogInformation(
                                                            "Recorded reminder #{Count} sent for invoice {InvoiceNumber}",
                                                            invoice.ReminderCount, invoice.InvoiceNumber);

                                                        return invoice;
                                                    }

                                public async Task<List<PaymentRecord>> GetPaymentRecordsAsync(Guid invoiceId, CancellationToken ct = default)
                                {
                                    await using var context = await _dbFactory.CreateDbContextAsync(ct);

                                    return await context.PaymentRecords
                                        .Include(pr => pr.RecordedBy)
                                        .Where(pr => pr.InvoiceId == invoiceId && pr.DateDeleted == null)
                                        .OrderByDescending(pr => pr.PaymentDate)
                                        .ToListAsync(ct);
                                }

                                public async Task<string> GenerateAccessTokenAsync(Guid invoiceId, CancellationToken ct = default)
                                {
                                    if (!_tenantContext.TenantId.HasValue)
                                        throw new InvalidOperationException("Tenant context is required");

                                    await using var context = await _dbFactory.CreateDbContextAsync(ct);

                                    var invoice = await context.Invoices
                                        .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
                                        ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

                                    // Generate a secure random token
                                    var tokenBytes = new byte[32];
                                    using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                                    {
                                        rng.GetBytes(tokenBytes);
                                    }
                                    var accessToken = Convert.ToBase64String(tokenBytes)
                                        .Replace("+", "-")
                                        .Replace("/", "_")
                                        .Replace("=", "");

                                    invoice.AccessToken = accessToken;
                                    invoice.AccessTokenExpires = null; // No expiration
                                    invoice.DateModified = DateTime.UtcNow;

                                    await context.SaveChangesAsync(ct);

                                    _logger.LogInformation("Generated access token for invoice {InvoiceNumber}", invoice.InvoiceNumber);
                                    return accessToken;
                                }

                                                                public async Task<Invoice?> GetByAccessTokenAsync(string accessToken, CancellationToken ct = default)
                                                                {
                                                                    if (string.IsNullOrWhiteSpace(accessToken))
                                                                        return null;

                                                                    await using var context = await _dbFactory.CreateDbContextAsync(ct);

                                                                    // Ignore tenant filter for public access
                                                                    return await context.Invoices
                                                                        .IgnoreQueryFilters()
                                                                        .Include(i => i.LineItems.Where(li => li.DateDeleted == null))
                                                                        .Include(i => i.ReserveStudy)
                                                                            .ThenInclude(rs => rs!.Community)
                                                                        .Include(i => i.PaymentRecords.Where(pr => pr.DateDeleted == null))
                                                                        .FirstOrDefaultAsync(i => 
                                                                            i.AccessToken == accessToken && 
                                                                            i.DateDeleted == null &&
                                                                            (i.AccessTokenExpires == null || i.AccessTokenExpires > DateTime.UtcNow), ct);
                                                                }

                                    public async Task<Invoice?> CreateNextMilestoneInvoiceAsync(
                                        Guid paidInvoiceId, 
                                        InvoiceMilestoneType nextMilestone, 
                                        CancellationToken ct = default)
                                    {
                                        if (!_tenantContext.TenantId.HasValue)
                                            throw new InvalidOperationException("Tenant context is required");

                                        await using var context = await _dbFactory.CreateDbContextAsync(ct);

                                        var paidInvoice = await context.Invoices
                                            .Include(i => i.LineItems.Where(li => li.DateDeleted == null))
                                            .Include(i => i.ReserveStudy)
                                            .FirstOrDefaultAsync(i => i.Id == paidInvoiceId && i.DateDeleted == null, ct);

                                        if (paidInvoice == null)
                                        {
                                            _logger.LogWarning("Cannot create next milestone: paid invoice {InvoiceId} not found", paidInvoiceId);
                                            return null;
                                        }

                                        if (paidInvoice.ReserveStudyId == Guid.Empty)
                                        {
                                            _logger.LogWarning("Cannot create next milestone: invoice has no reserve study");
                                            return null;
                                        }

                                        // Get tenant settings for defaults
                                        var settings = await GetOrCreateInvoiceSettingsAsync(context, ct);

                                        // Calculate the percentage/amount for the next milestone based on the total project
                                        var milestoneInfo = GetMilestoneInfo(nextMilestone);

                                        // Create the next invoice
                                        var nextInvoice = new Invoice
                                        {
                                            TenantId = _tenantContext.TenantId.Value,
                                            ReserveStudyId = paidInvoice.ReserveStudyId,
                                            InvoiceNumber = await GenerateInvoiceNumberAsync(ct),
                                            Status = InvoiceStatus.Draft,
                                            InvoiceDate = DateTime.UtcNow,
                                            DueDate = DateTime.UtcNow.AddDays(settings.DefaultNetDays),
                                            BillToName = paidInvoice.BillToName,
                                            BillToEmail = paidInvoice.BillToEmail,
                                            BillToAddress = paidInvoice.BillToAddress,
                                            BillToPhone = paidInvoice.BillToPhone,
                                            MilestoneType = nextMilestone,
                                            MilestoneDescription = milestoneInfo.Description,
                                            MilestonePercentage = milestoneInfo.Percentage,
                                            TaxRate = paidInvoice.TaxRate,
                                            Terms = paidInvoice.Terms,
                                            Notes = $"Auto-generated from payment of {paidInvoice.InvoiceNumber}",
                                            // Apply default payment terms from settings
                                            EarlyPaymentDiscountPercentage = settings.DefaultEarlyPaymentDiscount,
                                            LatePaymentInterestRate = settings.DefaultLateInterestRate
                                        };

                                        // Calculate early payment discount date if applicable
                                        if (nextInvoice.EarlyPaymentDiscountPercentage > 0)
                                        {
                                            nextInvoice.EarlyPaymentDiscountDate = nextInvoice.InvoiceDate.AddDays(settings.DefaultEarlyPaymentDays);
                                        }

                                        // Calculate late interest start date if applicable
                                        if (nextInvoice.LatePaymentInterestRate > 0)
                                        {
                                            nextInvoice.LateInterestStartDate = nextInvoice.DueDate.AddDays(settings.DefaultGracePeriodDays);
                                        }

                                        // For now, copy line items from the paid invoice proportionally
                                        // In a real scenario, you might calculate based on proposal milestones
                                        decimal subtotal = 0;
                                        foreach (var lineItem in paidInvoice.LineItems)
                                        {
                                            var newLineItem = new InvoiceLineItem
                                            {
                                                TenantId = _tenantContext.TenantId.Value,
                                                Description = $"{lineItem.Description} - {milestoneInfo.Description}",
                                                Quantity = lineItem.Quantity,
                                                UnitPrice = lineItem.UnitPrice, // Could be adjusted based on milestone percentage
                                                LineTotal = lineItem.LineTotal,
                                                SortOrder = lineItem.SortOrder
                                            };
                                            nextInvoice.LineItems.Add(newLineItem);
                                            subtotal += newLineItem.LineTotal;
                                        }

                                        nextInvoice.Subtotal = subtotal;
                                        nextInvoice.TaxAmount = subtotal * (nextInvoice.TaxRate / 100m);
                                        nextInvoice.TotalAmount = nextInvoice.Subtotal + nextInvoice.TaxAmount;

                                        if (nextInvoice.EarlyPaymentDiscountPercentage > 0)
                                        {
                                            nextInvoice.EarlyPaymentDiscountAmount = PaymentScheduleCalculator.CalculateEarlyPaymentDiscount(
                                                nextInvoice.TotalAmount, nextInvoice.EarlyPaymentDiscountPercentage);
                                        }

                                        context.Invoices.Add(nextInvoice);
                                        await context.SaveChangesAsync(ct);

                                        _logger.LogInformation(
                                            "Created next milestone invoice {InvoiceNumber} ({Milestone}) for {Amount:C}",
                                            nextInvoice.InvoiceNumber, nextMilestone, nextInvoice.TotalAmount);

                                        return nextInvoice;
                                    }

                                                                        private static (string Description, decimal Percentage) GetMilestoneInfo(InvoiceMilestoneType milestone)
                                                                        {
                                                                            return milestone switch
                                                                            {
                                                                                InvoiceMilestoneType.Deposit => ("Initial Deposit", 25m),
                                                                                InvoiceMilestoneType.SiteVisitComplete => ("Site Visit Complete", 25m),
                                                                                InvoiceMilestoneType.DraftReportDelivery => ("Draft Report Delivery", 25m),
                                                                                InvoiceMilestoneType.FinalDelivery => ("Final Delivery", 25m),
                                                                                InvoiceMilestoneType.FullPayment => ("Full Payment", 100m),
                                                                                InvoiceMilestoneType.Custom => ("Custom Milestone", 0m),
                                                                                _ => ("Payment", 0m)
                                                                            };
                                                                        }

                                        /// <summary>
                                        /// Sends an invoice to the client via email, marks it as sent, and generates access token.
                                        /// </summary>
                                        public async Task<Invoice> SendInvoiceAsync(Guid invoiceId, Guid sentByUserId, string baseUrl, CancellationToken ct = default)
                                        {
                                            if (!_tenantContext.TenantId.HasValue)
                                                throw new InvalidOperationException("Tenant context is required");

                                            await using var context = await _dbFactory.CreateDbContextAsync(ct);

                                            var invoice = await context.Invoices
                                                .Include(i => i.LineItems)
                                                .Include(i => i.ReserveStudy)
                                                    .ThenInclude(rs => rs!.Community)
                                                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == _tenantContext.TenantId.Value, ct)
                                                ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

                                            if (string.IsNullOrWhiteSpace(invoice.BillToEmail))
                                                throw new InvalidOperationException("Invoice has no recipient email address");

                                            // Generate access token if not already present
                                            if (string.IsNullOrEmpty(invoice.AccessToken))
                                            {
                                                invoice.AccessToken = Guid.NewGuid().ToString("N");
                                            }

                                            // Update status to sent
                                            if (invoice.Status == InvoiceStatus.Draft)
                                            {
                                                invoice.Status = InvoiceStatus.Finalized;
                                            }
                                            if (invoice.Status == InvoiceStatus.Finalized)
                                            {
                                                invoice.Status = InvoiceStatus.Sent;
                                            }

                                            invoice.SentAt = DateTime.UtcNow;
                                            invoice.SentByUserId = sentByUserId;
                                            invoice.DateModified = DateTime.UtcNow;

                                            await context.SaveChangesAsync(ct);

                                            // Send email
                                            try
                                            {
                                                var emailModel = new InvoiceEmail
                                                {
                                                    Invoice = invoice,
                                                    ReserveStudy = invoice.ReserveStudy,
                                                    BaseUrl = baseUrl.TrimEnd('/'),
                                                    InvoiceViewUrl = $"{baseUrl.TrimEnd('/')}/invoice/{invoice.AccessToken}"
                                                };

                                                var mailable = new InvoiceMailable(emailModel, invoice.BillToEmail);
                                                await _mailer.SendAsync(mailable);

                                                _logger.LogInformation("Sent invoice {InvoiceNumber} to {Email}", invoice.InvoiceNumber, invoice.BillToEmail);
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError(ex, "Failed to send invoice email for {InvoiceNumber} to {Email}", invoice.InvoiceNumber, invoice.BillToEmail);
                                                // Don't throw - invoice is already marked as sent, email failure shouldn't roll back
                                            }

                                            return invoice;
                                        }

                                        /// <summary>
                                        /// Attempts to auto-send an invoice if AutoSendOnCreate is enabled in tenant settings.
                                        /// </summary>
                                        private async Task TryAutoSendInvoiceAsync(Invoice invoice, ApplicationDbContext context, string? baseUrl, CancellationToken ct)
                                        {
                                            try
                                            {
                                                // Get tenant settings
                                                var settings = await context.TenantInvoiceSettings
                                                    .FirstOrDefaultAsync(s => s.TenantId == invoice.TenantId && s.DateDeleted == null, ct);

                                                if (settings == null || !settings.AutoSendOnCreate)
                                                {
                                                    return;
                                                }

                                                // Validate email address exists
                                                if (string.IsNullOrWhiteSpace(invoice.BillToEmail))
                                                {
                                                    _logger.LogWarning("AutoSendOnCreate: Cannot auto-send invoice {InvoiceNumber} - no recipient email", invoice.InvoiceNumber);
                                                    return;
                                                }

                                                // Determine base URL - use tenant domain if available
                                                var effectiveBaseUrl = baseUrl;
                                                if (string.IsNullOrEmpty(effectiveBaseUrl))
                                                {
                                                    var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == invoice.TenantId, ct);
                                                    if (tenant != null && !string.IsNullOrEmpty(tenant.Subdomain))
                                                    {
                                                        effectiveBaseUrl = $"https://{tenant.Subdomain}.alxreservecloud.com";
                                                    }
                                                    else
                                                    {
                                                        effectiveBaseUrl = "https://alxreservecloud.com";
                                                    }
                                                }

                                                // Generate access token
                                                if (string.IsNullOrEmpty(invoice.AccessToken))
                                                {
                                                    invoice.AccessToken = Guid.NewGuid().ToString("N");
                                                }

                                                // Update status
                                                if (invoice.Status == InvoiceStatus.Draft)
                                                {
                                                    invoice.Status = InvoiceStatus.Finalized;
                                                }
                                                if (invoice.Status == InvoiceStatus.Finalized)
                                                {
                                                    invoice.Status = InvoiceStatus.Sent;
                                                }

                                                invoice.SentAt = DateTime.UtcNow;
                                                invoice.DateModified = DateTime.UtcNow;

                                                await context.SaveChangesAsync(ct);

                                                // Send email
                                                var emailModel = new InvoiceEmail
                                                {
                                                    Invoice = invoice,
                                                    ReserveStudy = invoice.ReserveStudy,
                                                    BaseUrl = effectiveBaseUrl.TrimEnd('/'),
                                                    InvoiceViewUrl = $"{effectiveBaseUrl.TrimEnd('/')}/invoice/{invoice.AccessToken}"
                                                };

                                                var mailable = new InvoiceMailable(emailModel, invoice.BillToEmail);
                                                await _mailer.SendAsync(mailable);

                                                _logger.LogInformation("AutoSendOnCreate: Sent invoice {InvoiceNumber} to {Email}", invoice.InvoiceNumber, invoice.BillToEmail);
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError(ex, "AutoSendOnCreate: Failed to auto-send invoice {InvoiceNumber}", invoice.InvoiceNumber);
                                                // Don't throw - this is a best-effort operation
                                            }
                                        }
                                    }
