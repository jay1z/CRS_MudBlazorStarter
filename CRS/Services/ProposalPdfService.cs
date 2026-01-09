using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;
using PdfDocument = QuestPDF.Fluent.Document;

namespace CRS.Services;

public class ProposalPdfService : IProposalPdfService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ThemeService _themeService;

    public ProposalPdfService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        ThemeService themeService)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _themeService = themeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateProposalPdfAsync(Guid reserveStudyId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var study = await context.ReserveStudies
            .AsNoTracking()
            .Include(rs => rs.Community).ThenInclude(c => c!.PhysicalAddress)
            .Include(rs => rs.Contact)
            .Include(rs => rs.Specialist)
            .Include(rs => rs.CurrentProposal)
            .FirstOrDefaultAsync(rs => rs.Id == reserveStudyId);

        if (study?.CurrentProposal == null)
            throw new InvalidOperationException("Reserve study or proposal not found.");

        // Load effective PDF settings (merged from branding + PDF overrides)
        var settings = await GetEffectivePdfSettingsAsync();

        var document = PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginTop(settings.MarginTop);
                page.MarginBottom(settings.MarginBottom);
                page.MarginLeft(settings.MarginLeft);
                page.MarginRight(settings.MarginRight);
                page.DefaultTextStyle(x => x.FontSize(settings.BaseFontSize));

                page.Header().Element(c => ComposeHeader(c, study, settings));
                page.Content().Element(c => ComposeContent(c, study, settings));
                page.Footer().Element(c => ComposeFooter(c, settings));
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Creates effective PDF settings by merging BrandingPayload general settings with PdfSettings overrides
    /// </summary>
    private async Task<EffectivePdfSettings> GetEffectivePdfSettingsAsync()
    {
        var effective = new EffectivePdfSettings();
        PdfSettings? pdfSettings = null;
        ThemeService.BrandingPayload? branding = null;

        // Try to get settings from tenant branding
        if (!string.IsNullOrWhiteSpace(_tenantContext.BrandingJson))
        {
            if (_themeService.TryParseBranding(_tenantContext.BrandingJson, out var payload, out _))
            {
                branding = payload;
                pdfSettings = payload.PdfSettings;
            }
        }

        pdfSettings ??= PdfSettings.Default;

        // Resolve company info: PDF override > Branding > TenantContext
        effective.CompanyName = pdfSettings.CompanyNameOverride 
            ?? _tenantContext.TenantName;
        effective.CompanyTagline = pdfSettings.CompanyTaglineOverride 
            ?? branding?.CompanyTagline;
        effective.CompanyLogoUrl = pdfSettings.CompanyLogoUrlOverride 
            ?? branding?.CompanyLogoUrl;
        effective.CompanyPhone = branding?.CompanyPhone;
        effective.CompanyEmail = branding?.CompanyEmail;
        effective.CompanyWebsite = branding?.CompanyWebsite;
        effective.CompanyAddress = branding?.CompanyAddress;

        // Resolve colors: Use theme colors if enabled, otherwise PDF-specific colors
        if (pdfSettings.UseThemeColors && branding != null)
        {
            effective.PrimaryColor = branding.Primary ?? pdfSettings.PrimaryColor;
            effective.SecondaryColor = branding.Secondary ?? pdfSettings.SecondaryColor;
            // AccentColor uses Success from theme as it's typically used for positive indicators
            effective.AccentColor = branding.Success ?? pdfSettings.AccentColor;
        }
        else
        {
            effective.PrimaryColor = pdfSettings.PrimaryColor;
            effective.SecondaryColor = pdfSettings.SecondaryColor;
            effective.AccentColor = pdfSettings.AccentColor;
        }
        effective.TextColor = pdfSettings.TextColor;
        effective.MutedTextColor = pdfSettings.MutedTextColor;

        // Typography
        effective.FontFamily = pdfSettings.FontFamily;
        effective.BaseFontSize = pdfSettings.BaseFontSize;
        effective.HeaderFontSize = pdfSettings.HeaderFontSize;
        effective.SectionHeaderFontSize = pdfSettings.SectionHeaderFontSize;

        // Header & Footer
        effective.ShowLogoInHeader = pdfSettings.ShowLogoInHeader;
        effective.ShowCompanyNameInHeader = pdfSettings.ShowCompanyNameInHeader;
        effective.ShowFooter = pdfSettings.ShowFooter;
        effective.ShowPageNumbers = pdfSettings.ShowPageNumbers;
        effective.ShowGeneratedDate = pdfSettings.ShowGeneratedDate;
        effective.FooterText = pdfSettings.FooterText;
        effective.ShowContactInfoInFooter = pdfSettings.ShowContactInfoInFooter;

        // Margins
        effective.MarginTop = pdfSettings.MarginTop;
        effective.MarginBottom = pdfSettings.MarginBottom;
        effective.MarginLeft = pdfSettings.MarginLeft;
        effective.MarginRight = pdfSettings.MarginRight;

        // Document-specific settings
        effective.Proposal = pdfSettings.Proposal;
        effective.Report = pdfSettings.Report;

        return effective;
    }

    private void ComposeHeader(IContainer container, ReserveStudy study, EffectivePdfSettings settings)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(settings.Proposal.Title)
                        .FontSize(settings.HeaderFontSize).Bold().FontColor(settings.PrimaryColor);
                    col.Item().Text($"Proposal Date: {study.CurrentProposal!.ProposalDate:MMMM dd, yyyy}")
                        .FontSize(10).FontColor(settings.MutedTextColor);
                    
                    // Show company name if configured
                    if (settings.ShowCompanyNameInHeader && !string.IsNullOrEmpty(settings.CompanyName))
                    {
                        col.Item().Text(settings.CompanyName)
                            .FontSize(12).FontColor(settings.TextColor);
                    }
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(2).LineColor(settings.PrimaryColor);
        });
    }

    private void ComposeContent(IContainer container, ReserveStudy study, EffectivePdfSettings settings)
    {
        var proposalSettings = settings.Proposal;
        
        container.PaddingVertical(20).Column(column =>
        {
            // Community Information
            if (proposalSettings.ShowCommunityInfo)
            {
                column.Item().Element(c => ComposeSection(c, "Community Information", settings, col =>
                {
                    col.Item().Text($"Community: {study.Community?.Name ?? "N/A"}").Bold();
                    col.Item().Text($"Address: {study.Community?.PhysicalAddress?.FullAddress ?? "N/A"}");
                }));

                column.Item().Height(20);
            }

            // Contact Information
            if (proposalSettings.ShowContactInfo)
            {
                column.Item().Element(c => ComposeSection(c, "Primary Contact", settings, col =>
                {
                    col.Item().Text($"Name: {study.Contact?.FullName ?? "N/A"}");
                    col.Item().Text($"Email: {study.Contact?.Email ?? "N/A"}");
                    col.Item().Text($"Phone: {study.Contact?.Phone ?? "N/A"}");
                }));

                column.Item().Height(20);
            }

            // Proposal Details
            column.Item().Element(c => ComposeSection(c, "Proposal Details", settings, col =>
            {
                if (proposalSettings.ShowScopeOfWork)
                {
                    col.Item().Text("Scope of Work").Bold().FontSize(12);
                    col.Item().PaddingTop(5).Text(study.CurrentProposal!.ProposalScope ?? "N/A");
                    col.Item().Height(15);
                }

                if (proposalSettings.ShowEstimatedCost)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Estimated Cost").Bold();
                            c.Item().Text($"${study.CurrentProposal!.EstimatedCost:N2}")
                                .FontSize(18).Bold().FontColor(settings.AccentColor);
                        });
                    });
                }

                if (!string.IsNullOrEmpty(study.CurrentProposal!.Comments))
                {
                    col.Item().Height(15);
                    col.Item().Text("Additional Comments").Bold().FontSize(12);
                    col.Item().PaddingTop(5).Text(study.CurrentProposal.Comments);
                }
            }));

            column.Item().Height(20);

            // Service Level Section
            if (proposalSettings.ShowServiceLevel && !string.IsNullOrEmpty(study.CurrentProposal!.ServiceLevel))
            {
                column.Item().Element(c => ComposeSection(c, "Service Level", settings, col =>
                {
                    var serviceLevelDescription = GetServiceLevelDescription(study.CurrentProposal.ServiceLevel);
                    col.Item().Text(serviceLevelDescription).FontSize(settings.BaseFontSize);
                }));

                column.Item().Height(20);
            }

            // Delivery Timeframe Section
            if (proposalSettings.ShowDeliveryTimeframe && !string.IsNullOrEmpty(study.CurrentProposal.DeliveryTimeframe))
            {
                column.Item().Element(c => ComposeSection(c, "Delivery Timeframe", settings, col =>
                {
                    var timeframeDescription = GetDeliveryTimeframeDescription(study.CurrentProposal.DeliveryTimeframe);
                    col.Item().Text(timeframeDescription).FontSize(settings.BaseFontSize);
                }));

                column.Item().Height(20);
            }

            // Additional Services Section
            if (proposalSettings.ShowAdditionalServices)
            {
                var additionalServices = GetAdditionalServices(study.CurrentProposal);
                if (additionalServices.Count > 0)
                {
                    column.Item().Element(c => ComposeSection(c, "Additional Services Included", settings, col =>
                    {
                        foreach (var service in additionalServices)
                        {
                            col.Item().Row(row =>
                            {
                                row.ConstantItem(15).AlignMiddle().Text("✓").FontColor(settings.AccentColor).Bold();
                                row.RelativeItem().Text(service);
                            });
                        }
                    }));

                    column.Item().Height(20);
                }
            }

            // Payment Terms Section
            if (proposalSettings.ShowPaymentTerms && !string.IsNullOrEmpty(study.CurrentProposal.PaymentTerms))
            {
                column.Item().Element(c => ComposeSection(c, "Payment Terms", settings, col =>
                {
                    col.Item().Text(study.CurrentProposal.PaymentTerms);
                }));

                column.Item().Height(20);
            }

            // Specialist Information
            if (proposalSettings.ShowSpecialistInfo && study.Specialist != null)
            {
                column.Item().Element(c => ComposeSection(c, "Your Reserve Specialist", settings, col =>
                {
                    col.Item().Text($"Name: {study.Specialist.FullName}");
                    col.Item().Text($"Email: {study.Specialist.Email}");
                }));
            }

            column.Item().Height(30);

            // Terms and Conditions
            if (proposalSettings.ShowTermsAndConditions && proposalSettings.TermsAndConditions.Count > 0)
            {
                column.Item().Element(c => ComposeSection(c, "Terms and Conditions", settings, col =>
                {
                    for (int i = 0; i < proposalSettings.TermsAndConditions.Count; i++)
                    {
                        col.Item().Text($"{i + 1}. {proposalSettings.TermsAndConditions[i]}");
                    }
                }));

                column.Item().Height(20);
            }

            // Electronic Acceptance Notice
            if (proposalSettings.ShowElectronicAcceptanceNotice)
            {
                var acceptanceText = proposalSettings.ElectronicAcceptanceText 
                    ?? "This proposal can be accepted electronically through our secure online portal. Upon acceptance, you will receive a confirmation email with next steps.";
                
                // Use a light gray background instead of trying to create transparent primary color
                column.Item().Background(Colors.Grey.Lighten4).Padding(15).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(24).AlignMiddle().Text("ℹ").FontSize(16).FontColor(settings.PrimaryColor);
                        row.RelativeItem().Text("Electronic Acceptance")
                            .Bold().FontSize(12).FontColor(settings.PrimaryColor);
                    });
                    col.Item().Height(8);
                    col.Item().Text(acceptanceText)
                        .FontSize(10).FontColor(settings.MutedTextColor);
                });
            }
        });
    }

    private static string GetServiceLevelDescription(string serviceLevel)
    {
        return serviceLevel switch
        {
            "Level1" => "Level I: Full Service - Includes comprehensive site inspection and complete financial analysis with 30-year funding projections.",
            "Level2" => "Level II: Update with Site Visit - Includes site inspection and update of previous reserve study with current component conditions.",
            "Level3" => "Level III: Update without Site Visit - Update of previous reserve study based on existing documentation without new inspection.",
            _ => serviceLevel
        };
    }

    private static string GetDeliveryTimeframeDescription(string timeframe)
    {
        return timeframe switch
        {
            "Standard" => "Standard Delivery: 4-6 weeks from data collection completion.",
            "Expedited" => "Expedited Delivery: 2-3 weeks from data collection completion (additional fee applies).",
            "Rush" => "Rush Delivery: 7-10 business days from data collection completion (premium fee applies).",
            _ => timeframe
        };
    }

    private static List<string> GetAdditionalServices(Proposal proposal)
    {
        var services = new List<string>();
        
        if (proposal.IncludePrepaymentDiscount)
            services.Add("Prepayment Discount (5% off total cost)");
        
        if (proposal.IncludeDigitalDelivery)
            services.Add("Digital Delivery Only (eco-friendly PDF format)");
        
        if (proposal.IncludeComponentInventory)
            services.Add("Detailed Component Inventory with photos and condition assessments");
        
        if (proposal.IncludeFundingPlans)
            services.Add("Alternative Funding Plans (multiple scenarios for board consideration)");
        
        return services;
    }

    private void ComposeSection(IContainer container, string title, EffectivePdfSettings settings, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(5)
                .Text(title).Bold().FontSize(settings.SectionHeaderFontSize).FontColor(settings.PrimaryColor);
            column.Item().PaddingTop(10).Column(content);
        });
    }

    private void ComposeFooter(IContainer container, EffectivePdfSettings settings)
    {
        if (!settings.ShowFooter)
            return;

        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(10).Row(row =>
            {
                if (settings.ShowGeneratedDate)
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Generated on ").FontSize(9).FontColor(settings.MutedTextColor);
                        text.Span($"{DateTime.Now:MMMM dd, yyyy}").FontSize(9).FontColor(settings.MutedTextColor);
                    });
                }
                else if (!string.IsNullOrEmpty(settings.FooterText))
                {
                    row.RelativeItem().Text(settings.FooterText).FontSize(9).FontColor(settings.MutedTextColor);
                }
                else
                {
                    row.RelativeItem();
                }

                if (settings.ShowPageNumbers)
                {
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(9).FontColor(settings.MutedTextColor);
                        text.CurrentPageNumber().FontSize(9).FontColor(settings.MutedTextColor);
                        text.Span(" of ").FontSize(9).FontColor(settings.MutedTextColor);
                        text.TotalPages().FontSize(9).FontColor(settings.MutedTextColor);
                    });
                }
            });

            // Company contact info in footer if configured
            if (settings.ShowContactInfoInFooter)
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(settings.CompanyPhone)) parts.Add(settings.CompanyPhone);
                if (!string.IsNullOrEmpty(settings.CompanyEmail)) parts.Add(settings.CompanyEmail);
                if (!string.IsNullOrEmpty(settings.CompanyWebsite)) parts.Add(settings.CompanyWebsite);
                
                if (parts.Count > 0)
                {
                    column.Item().PaddingTop(5).AlignCenter().Text(text =>
                    {
                        text.Span(string.Join(" | ", parts)).FontSize(8).FontColor(settings.MutedTextColor);
                    });
                }
            }
        });
    }
}
