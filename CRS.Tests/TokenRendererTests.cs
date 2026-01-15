using CRS.Models.NarrativeTemplates;
using CRS.Services.NarrativeReport;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace CRS.Tests;

/// <summary>
/// Unit tests for <see cref="DefaultTokenRenderer"/>.
/// Tests token rendering with various context data scenarios.
/// </summary>
public class TokenRendererTests
{
    private readonly DefaultTokenRenderer _renderer;
    private readonly TokenRenderOptions _options;

    public TokenRendererTests()
    {
        var logger = new Mock<ILogger<DefaultTokenRenderer>>();
        _renderer = new DefaultTokenRenderer(logger.Object);
        _options = TokenRenderOptions.Default;
    }

    #region PAGE_BREAK Tests

    [Fact]
    public void RenderToken_PageBreak_ReturnsPageBreakDiv()
    {
        // Arrange
        var context = CreateMinimalContext();

        // Act
        var result = _renderer.RenderToken("PAGE_BREAK", context, _options);

        // Assert
        Assert.Equal("<div class=\"page-break\"></div>", result);
    }

    #endregion

    #region TABLE:ContributionSchedule Tests

    [Fact]
    public void RenderToken_ContributionSchedule_EmptySchedule_ReturnsNoDataMessage()
    {
        // Arrange
        var context = CreateMinimalContext();

        // Act
        var result = _renderer.RenderToken("TABLE:ContributionSchedule", context, _options);

        // Assert
        Assert.Contains("No contribution schedule available", result);
    }

    [Fact]
    public void RenderToken_ContributionSchedule_WithData_ReturnsTable()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.CalculatedOutputs.ContributionSchedule =
        [
            new YearContribution { Year = 2025, Amount = 50000m },
            new YearContribution { Year = 2026, Amount = 52500m },
            new YearContribution { Year = 2027, Amount = 55125m }
        ];

        // Act
        var result = _renderer.RenderToken("TABLE:ContributionSchedule", context, _options);

        // Assert
        Assert.Contains("<table class=\"table\">", result);
        Assert.Contains("<th>Year</th>", result);
        Assert.Contains("<th>Recommended Reserve Contribution</th>", result);
        Assert.Contains("<td>2025</td>", result);
        Assert.Contains("$50,000", result);
    }

    [Fact]
    public void RenderToken_ContributionSchedule_Condensed_ShowsCorrectYears()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.CalculatedOutputs.ContributionSchedule = Enumerable.Range(2025, 30)
            .Select(y => new YearContribution { Year = y, Amount = 50000m + (y - 2025) * 1000 })
            .ToList();

        var options = new TokenRenderOptions
        {
            CondenseContributionTable = true,
            ContributionScheduleYearsToShow = 6
        };

        // Act
        var result = _renderer.RenderToken("TABLE:ContributionSchedule", context, options);

        // Assert
        // Should show first 6 years: 2025-2030
        Assert.Contains("<td>2025</td>", result);
        Assert.Contains("<td>2030</td>", result);
        // Should show final year: 2054
        Assert.Contains("<td>2054</td>", result);
        // Should NOT show every year (e.g., 2031-2034)
        Assert.DoesNotContain("<td>2031</td>", result);
        Assert.DoesNotContain("<td>2032</td>", result);
        Assert.DoesNotContain("<td>2033</td>", result);
    }

    #endregion

    #region TABLE:InfoFurnished Tests

    [Fact]
    public void RenderToken_InfoFurnished_ReturnsFormattedTable()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.CalculatedOutputs.FirstYear = new FirstYearSummary
        {
            StartingBalance = 100000m,
            Contribution = 50000m,
            Interest = 2000m,
            Expenditures = 30000m,
            EndingBalance = 122000m
        };
        context.Study.FiscalYearStart = new DateTime(2025, 1, 1);

        // Act
        var result = _renderer.RenderToken("TABLE:InfoFurnished", context, _options);

        // Assert
        Assert.Contains("<table class=\"table\">", result);
        Assert.Contains("Starting Reserve Cash Balance", result);
        Assert.Contains("Budgeted Reserve Contributions", result);
        Assert.Contains("Anticipated Interest Earned", result);
        Assert.Contains("Less: Anticipated Expenditures", result);
        Assert.Contains("Anticipated Year-End Reserve Cash Balance", result);
        Assert.Contains("$100,000", result);
        Assert.Contains("$50,000", result);
        Assert.Contains("$122,000", result);
    }

    #endregion

    #region SIGNATURES Tests

    [Fact]
    public void RenderToken_Signatures_NoSignatories_ReturnsMessage()
    {
        // Arrange
        var context = CreateMinimalContext();

        // Act
        var result = _renderer.RenderToken("SIGNATURES", context, _options);

        // Assert
        Assert.Contains("No signatories defined", result);
    }

    [Fact]
    public void RenderToken_Signatures_WithSignatory_ReturnsBlock()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.Signatories =
        [
            new SignatoryInfo
            {
                Name = "John Smith",
                Title = "Reserve Specialist",
                Credentials = "RS, PRA"
            }
        ];

        // Act
        var result = _renderer.RenderToken("SIGNATURES", context, _options);

        // Assert
        Assert.Contains("<div class=\"signature-container\">", result);
        Assert.Contains("<strong>John Smith</strong>", result);
        Assert.Contains("Reserve Specialist, RS, PRA", result);
    }

    [Fact]
    public void RenderToken_Signatures_WithImage_IncludesImage()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.Signatories =
        [
            new SignatoryInfo
            {
                Name = "Jane Doe",
                Title = "Analyst",
                SignatureImageUrl = "https://example.com/signature.png"
            }
        ];

        var options = new TokenRenderOptions { AllowImages = true };

        // Act
        var result = _renderer.RenderToken("SIGNATURES", context, options);

        // Assert
        Assert.Contains("<img src=\"https://example.com/signature.png\"", result);
    }

    [Fact]
    public void RenderToken_Signatures_ImagesDisabled_ShowsLine()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.Signatories =
        [
            new SignatoryInfo
            {
                Name = "Jane Doe",
                SignatureImageUrl = "https://example.com/signature.png"
            }
        ];

        var options = new TokenRenderOptions { AllowImages = false };

        // Act
        var result = _renderer.RenderToken("SIGNATURES", context, options);

        // Assert
        Assert.DoesNotContain("<img", result);
        Assert.Contains("border-bottom: 1px solid", result);
    }

    #endregion

    #region PHOTOS Tests

    [Fact]
    public void RenderToken_Photos_NoPhotos_ReturnsMessage()
    {
        // Arrange
        var context = CreateMinimalContext();

        // Act
        var result = _renderer.RenderToken("PHOTOS", context, _options);

        // Assert
        Assert.Contains("No photos available", result);
    }

    [Fact]
    public void RenderToken_Photos_WithPhotos_ReturnsTableLayout()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.Photos =
        [
            new PhotoItem { Url = "https://example.com/photo1.jpg", Caption = "Roof view", SortOrder = 1 },
            new PhotoItem { Url = "https://example.com/photo2.jpg", Caption = "Pool area", SortOrder = 2 },
            new PhotoItem { Url = "https://example.com/photo3.jpg", Caption = "Parking lot", SortOrder = 3 }
        ];

        // Act
        var result = _renderer.RenderToken("PHOTOS", context, _options);

        // Assert
        Assert.Contains("<table style=\"width:100%; border-collapse:collapse;\">", result);
        Assert.Contains("<img src=\"https://example.com/photo1.jpg\"", result);
        Assert.Contains("Roof view", result);
        Assert.Contains("Pool area", result);
    }

    [Fact]
    public void RenderToken_Photos_ImagesDisabled_ReturnsMessage()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.Photos =
        [
            new PhotoItem { Url = "https://example.com/photo1.jpg", Caption = "Test" }
        ];

        var options = new TokenRenderOptions { AllowImages = false };

        // Act
        var result = _renderer.RenderToken("PHOTOS", context, options);

        // Assert
        Assert.Contains("No photos available", result);
    }

    #endregion

    #region VENDORS Tests

    [Fact]
    public void RenderToken_Vendors_NoVendors_ReturnsEmpty()
    {
        // Arrange
        var context = CreateMinimalContext();

        // Act
        var result = _renderer.RenderToken("VENDORS", context, _options);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void RenderToken_Vendors_WithVendors_ReturnsTable()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.Vendors =
        [
            new VendorInfo
            {
                Name = "ABC Roofing",
                Category = "Roofing",
                Phone = "555-1234",
                Email = "info@abcroofing.com"
            },
            new VendorInfo
            {
                Name = "XYZ Painting",
                Category = "Painting",
                Phone = "555-5678"
            }
        ];

        // Act
        var result = _renderer.RenderToken("VENDORS", context, _options);

        // Assert
        Assert.Contains("<table class=\"table\">", result);
        Assert.Contains("<th>Vendor</th>", result);
        Assert.Contains("<th>Service</th>", result);
        Assert.Contains("<th>Contact</th>", result);
        Assert.Contains("ABC Roofing", result);
        Assert.Contains("555-1234 | info@abcroofing.com", result);
    }

    #endregion

    #region GLOSSARY Tests

    [Fact]
    public void RenderToken_Glossary_NoTerms_ReturnsEmpty()
    {
        // Arrange
        var context = CreateMinimalContext();

        // Act
        var result = _renderer.RenderToken("GLOSSARY", context, _options);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void RenderToken_Glossary_WithTerms_ReturnsTable()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.GlossaryTerms =
        [
            new GlossaryTerm { Term = "Reserve Fund", Definition = "A fund set aside for future major repairs." },
            new GlossaryTerm { Term = "Useful Life", Definition = "The estimated time a component will remain functional." }
        ];

        // Act
        var result = _renderer.RenderToken("GLOSSARY", context, _options);

        // Assert
        Assert.Contains("<table class=\"table\">", result);
        Assert.Contains("<th style=\"width: 25%;\">Term</th>", result);
        Assert.Contains("<th style=\"width: 75%;\">Definition</th>", result);
        Assert.Contains("Reserve Fund", result);
        Assert.Contains("Useful Life", result);
        // Should be sorted alphabetically
        var reserveIndex = result.IndexOf("Reserve Fund");
        var usefulIndex = result.IndexOf("Useful Life");
        Assert.True(reserveIndex < usefulIndex);
    }

    #endregion

    #region ReplaceAllTokens Tests

    [Fact]
    public void ReplaceAllTokens_MultipleTokens_ReplacesAll()
    {
        // Arrange
        var html = @"<div>
[[PAGE_BREAK]]
<h1>Contribution Schedule</h1>
[[TABLE:ContributionSchedule]]
<h1>Signatures</h1>
[[SIGNATURES]]
</div>";

        var context = CreateMinimalContext();
        context.CalculatedOutputs.ContributionSchedule =
        [
            new YearContribution { Year = 2025, Amount = 50000m }
        ];
        context.Signatories =
        [
            new SignatoryInfo { Name = "John Smith", Title = "Specialist" }
        ];

        // Act
        var result = _renderer.ReplaceAllTokens(html, context, _options);

        // Assert
        Assert.DoesNotContain("[[", result);
        Assert.Contains("<div class=\"page-break\"></div>", result);
        Assert.Contains("<table class=\"table\">", result);
        Assert.Contains("John Smith", result);
    }

    [Fact]
    public void ReplaceAllTokens_UnknownToken_ReplacesWithEmpty()
    {
        // Arrange
        var html = @"<div>[[UNKNOWN_TOKEN]]</div>";
        var context = CreateMinimalContext();

        // Act
        var result = _renderer.ReplaceAllTokens(html, context, _options);

        // Assert
        Assert.Equal("<div></div>", result);
    }

    #endregion

    #region HTML Encoding Tests

    [Fact]
    public void RenderToken_Vendors_HtmlEncodesSpecialCharacters()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.Vendors =
        [
            new VendorInfo
            {
                Name = "Smith & Sons <Roofing>",
                Category = "Roofing"
            }
        ];

        // Act
        var result = _renderer.RenderToken("VENDORS", context, _options);

        // Assert
        Assert.Contains("Smith &amp; Sons &lt;Roofing&gt;", result);
        Assert.DoesNotContain("<Roofing>", result);
    }

    [Fact]
    public void RenderToken_Glossary_HtmlEncodesDefinitions()
    {
        // Arrange
        var context = CreateMinimalContext();
        context.GlossaryTerms =
        [
            new GlossaryTerm
            {
                Term = "Test Term",
                Definition = "Contains <script>alert('xss')</script> attempt"
            }
        ];

        // Act
        var result = _renderer.RenderToken("GLOSSARY", context, _options);

        // Assert
        Assert.Contains("&lt;script&gt;", result);
        Assert.DoesNotContain("<script>", result);
    }

    #endregion

    #region Helper Methods

    private static ReserveStudyReportContext CreateMinimalContext()
    {
        return new ReserveStudyReportContext
        {
            Association = new AssociationInfo { Name = "Test Community" },
            Study = new StudyInfo { ReportTitle = "Test Report" },
            FinancialAssumptions = new FinancialAssumptions(),
            CalculatedOutputs = new CalculatedOutputs
            {
                FirstYear = new FirstYearSummary(),
                ContributionSchedule = []
            },
            Branding = new BrandingInfo { CompanyName = "Test Company" },
            Signatories = [],
            Vendors = [],
            GlossaryTerms = [],
            Photos = []
        };
    }

    #endregion
}

/// <summary>
/// Tests for <see cref="TokenRenderHelpers"/>.
/// </summary>
public class TokenRenderHelpersTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("Hello", "Hello")]
    [InlineData("<script>", "&lt;script&gt;")]
    [InlineData("Smith & Sons", "Smith &amp; Sons")]
    [InlineData("\"quoted\"", "&quot;quoted&quot;")]
    public void HtmlEncode_EncodesCorrectly(string? input, string expected)
    {
        var result = TokenRenderHelpers.HtmlEncode(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Money_FormatsWithCurrency()
    {
        var options = new TokenRenderOptions
        {
            IncludeCurrencySymbol = true,
            MoneyDecimals = 0,
            CultureName = "en-US"
        };
        Assert.Equal("$1,235", TokenRenderHelpers.Money(1234.56m, options));
    }

    [Fact]
    public void Money_FormatsWithDecimals()
    {
        var options = new TokenRenderOptions
        {
            IncludeCurrencySymbol = true,
            MoneyDecimals = 2,
            CultureName = "en-US"
        };
        Assert.Equal("$1,234.56", TokenRenderHelpers.Money(1234.56m, options));
    }

    [Fact]
    public void Money_FormatsWithoutCurrency()
    {
        var options = new TokenRenderOptions
        {
            IncludeCurrencySymbol = false,
            MoneyDecimals = 0,
            CultureName = "en-US"
        };
        Assert.Equal("1,235", TokenRenderHelpers.Money(1234.56m, options));
    }

    [Fact]
    public void Money_NullValue_ReturnsEmpty()
    {
        var options = TokenRenderOptions.Default;
        Assert.Equal("", TokenRenderHelpers.Money(null, options));
    }

    [Fact]
    public void Percent_FormatsDecimalToPercentage()
    {
        var options = new TokenRenderOptions
        {
            PercentDecimals = 1,
            CultureName = "en-US"
        };
        Assert.Equal("3.0%", TokenRenderHelpers.Percent(0.03m, options, false));
    }

    [Fact]
    public void Percent_RoundsCorrectly()
    {
        var options = new TokenRenderOptions
        {
            PercentDecimals = 0,
            CultureName = "en-US"
        };
        Assert.Equal("16%", TokenRenderHelpers.Percent(0.155m, options, false));
    }

    [Fact]
    public void Percent_AlreadyPercentage_DoesNotMultiply()
    {
        var options = new TokenRenderOptions
        {
            PercentDecimals = 1,
            CultureName = "en-US"
        };
        Assert.Equal("5.5%", TokenRenderHelpers.Percent(5.5m, options, true));
    }

    [Fact]
    public void Percent_NullValue_ReturnsEmpty()
    {
        var options = TokenRenderOptions.Default;
        Assert.Equal("", TokenRenderHelpers.Percent(null, options, false));
    }

    [Fact]
    public void Date_FormatsCorrectly()
    {
        var date = new DateTime(2025, 3, 15);
        var result = TokenRenderHelpers.Date(date);
        Assert.Equal("March 15, 2025", result);
    }

    [Fact]
    public void Date_NullValue_ReturnsEmpty()
    {
        var result = TokenRenderHelpers.Date(null);
        Assert.Empty(result);
    }

    [Fact]
    public void JoinNonEmpty_FiltersEmptyValues()
    {
        var result = TokenRenderHelpers.JoinNonEmpty(", ", "A", null, "B", "", "C");
        Assert.Equal("A, B, C", result);
    }
}
