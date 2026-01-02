namespace CRS.Services.Email;

/// <summary>
/// Provides modern HTML email template wrappers matching the site's design.
/// Uses inline CSS for maximum email client compatibility.
/// </summary>
public static class EmailTemplateBuilder
{
    // Brand colors matching the site's gradient theme
    private const string PrimaryGradient = "linear-gradient(135deg, #667eea 0%, #764ba2 100%)";
    private const string PrimaryColor = "#667eea";
    private const string SecondaryColor = "#764ba2";
    private const string SuccessColor = "#10b981";
    private const string WarningColor = "#f59e0b";
    private const string TextColor = "#374151";
    private const string MutedTextColor = "#6b7280";
    private const string BackgroundColor = "#f3f4f6";
    private const string CardBackground = "#ffffff";
    private const string BorderColor = "#e5e7eb";

    /// <summary>
    /// Wraps content in a modern styled email template.
    /// </summary>
    public static string WrapInTemplate(string title, string bodyContent, string? footerText = null, string? companyName = null)
    {
        var company = companyName ?? "ALX Reserve Cloud";
        var footer = footerText ?? $"© {DateTime.UtcNow.Year} {company}. All rights reserved.";
        
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <title>{title}</title>
    <!--[if mso]>
    <noscript>
        <xml>
            <o:OfficeDocumentSettings>
                <o:PixelsPerInch>96</o:PixelsPerInch>
            </o:OfficeDocumentSettings>
        </xml>
    </noscript>
    <![endif]-->
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: {BackgroundColor}; -webkit-font-smoothing: antialiased;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: {BackgroundColor};"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""600"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 600px; width: 100%;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: {PrimaryGradient}; border-radius: 16px 16px 0 0; padding: 32px 40px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;"">{title}</h1>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style=""background-color: {CardBackground}; padding: 40px; border-left: 1px solid {BorderColor}; border-right: 1px solid {BorderColor};"">
                            {bodyContent}
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: {CardBackground}; padding: 24px 40px; border-radius: 0 0 16px 16px; border: 1px solid {BorderColor}; border-top: none; text-align: center;"">
                            <p style=""margin: 0; color: {MutedTextColor}; font-size: 13px;"">{footer}</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    /// <summary>
    /// Creates a modern styled primary action button.
    /// </summary>
    public static string CreateButton(string text, string url, bool isPrimary = true)
    {
        var bgColor = isPrimary ? PrimaryColor : "transparent";
        var textColor = isPrimary ? "#ffffff" : PrimaryColor;
        var border = isPrimary ? "none" : $"2px solid {PrimaryColor}";
        
        return $@"
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" align=""center"" style=""margin: 24px auto;"">
    <tr>
        <td style=""border-radius: 8px; background: {bgColor};"">
            <a href=""{url}"" target=""_blank"" style=""display: inline-block; padding: 16px 32px; font-size: 16px; font-weight: 600; color: {textColor}; text-decoration: none; border-radius: 8px; border: {border};"">{text}</a>
        </td>
    </tr>
</table>";
    }

    /// <summary>
    /// Creates a success-colored button (green).
    /// </summary>
    public static string CreateSuccessButton(string text, string url)
    {
        return $@"
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" align=""center"" style=""margin: 24px auto;"">
    <tr>
        <td style=""border-radius: 8px; background: {SuccessColor};"">
            <a href=""{url}"" target=""_blank"" style=""display: inline-block; padding: 16px 32px; font-size: 16px; font-weight: 600; color: #ffffff; text-decoration: none; border-radius: 8px;"">{text}</a>
        </td>
    </tr>
</table>";
    }

    /// <summary>
    /// Creates an info/highlight box.
    /// </summary>
    public static string CreateInfoBox(string content, string? title = null)
    {
        var titleHtml = string.IsNullOrEmpty(title) ? "" : $@"<p style=""margin: 0 0 8px 0; font-weight: 600; color: {PrimaryColor};"">{title}</p>";
        
        return $@"
<div style=""background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%); border-left: 4px solid {PrimaryColor}; border-radius: 0 8px 8px 0; padding: 20px; margin: 24px 0;"">
    {titleHtml}
    <div style=""color: {TextColor}; font-size: 15px; line-height: 1.6;"">{content}</div>
</div>";
    }

    /// <summary>
    /// Creates a warning/alert box.
    /// </summary>
    public static string CreateWarningBox(string content)
    {
        return $@"
<div style=""background-color: #fef3c7; border-left: 4px solid {WarningColor}; border-radius: 0 8px 8px 0; padding: 16px 20px; margin: 24px 0;"">
    <p style=""margin: 0; color: #92400e; font-size: 14px;"">⚠️ {content}</p>
</div>";
    }

    /// <summary>
    /// Creates a success box.
    /// </summary>
    public static string CreateSuccessBox(string content)
    {
        return $@"
<div style=""background-color: #d1fae5; border-left: 4px solid {SuccessColor}; border-radius: 0 8px 8px 0; padding: 16px 20px; margin: 24px 0;"">
    <p style=""margin: 0; color: #065f46; font-size: 14px;"">✓ {content}</p>
</div>";
    }

    /// <summary>
    /// Creates a data row for displaying key-value pairs.
    /// </summary>
    public static string CreateDataRow(string label, string value)
    {
        return $@"
<tr>
    <td style=""padding: 12px 0; border-bottom: 1px solid {BorderColor};"">
        <span style=""color: {MutedTextColor}; font-size: 14px;"">{label}</span>
    </td>
    <td style=""padding: 12px 0; border-bottom: 1px solid {BorderColor}; text-align: right;"">
        <span style=""color: {TextColor}; font-size: 14px; font-weight: 500;"">{value}</span>
    </td>
</tr>";
    }

    /// <summary>
    /// Creates a section header.
    /// </summary>
    public static string CreateSectionHeader(string title)
    {
        return $@"
<h2 style=""margin: 32px 0 16px 0; color: {TextColor}; font-size: 18px; font-weight: 600; border-bottom: 2px solid {PrimaryColor}; padding-bottom: 8px;"">{title}</h2>";
    }

    /// <summary>
    /// Creates styled paragraph text.
    /// </summary>
    public static string CreateParagraph(string text)
    {
        return $@"<p style=""margin: 0 0 16px 0; color: {TextColor}; font-size: 15px; line-height: 1.6;"">{text}</p>";
    }

    /// <summary>
    /// Creates a styled link.
    /// </summary>
    public static string CreateLink(string text, string url)
    {
        return $@"<a href=""{url}"" style=""color: {PrimaryColor}; text-decoration: none; font-weight: 500;"">{text}</a>";
    }

    /// <summary>
    /// Creates a credentials display box for login info.
    /// </summary>
    public static string CreateCredentialsBox(string email, string? temporaryPassword = null)
    {
        var passwordRow = string.IsNullOrEmpty(temporaryPassword) ? "" : $@"
<tr>
    <td style=""padding: 8px 16px; color: {MutedTextColor}; font-size: 13px; white-space: nowrap;"">Password:</td>
    <td style=""padding: 8px 16px;"">
        <code style=""background: #f3f4f6; padding: 6px 12px; border-radius: 6px; font-family: 'SF Mono', Monaco, 'Courier New', monospace; font-size: 15px; letter-spacing: 1px; color: {TextColor};"">{temporaryPassword}</code>
    </td>
</tr>";
        
        return $@"
<div style=""background: linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%); border: 1px solid {BorderColor}; border-radius: 12px; padding: 20px; margin: 24px 0;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
        <tr>
            <td style=""padding: 8px 16px; color: {MutedTextColor}; font-size: 13px; white-space: nowrap;"">Email:</td>
            <td style=""padding: 8px 16px;"">
                <code style=""background: #f3f4f6; padding: 6px 12px; border-radius: 6px; font-family: 'SF Mono', Monaco, 'Courier New', monospace; font-size: 14px; color: {TextColor};"">{email}</code>
            </td>
        </tr>
        {passwordRow}
    </table>
</div>";
    }

    /// <summary>
    /// Creates a simple confirmation email body for account verification.
    /// </summary>
    public static string CreateConfirmationEmail(string confirmationLink)
    {
        var body = $@"
{CreateParagraph("Thank you for creating an account! Please confirm your email address to activate your account and get started.")}
{CreateButton("Confirm Email Address", confirmationLink)}
{CreateParagraph("If you didn't create this account, you can safely ignore this email.")}
<p style=""margin: 24px 0 0 0; color: {MutedTextColor}; font-size: 13px; text-align: center;"">
    Button not working? Copy and paste this link into your browser:<br>
    <a href=""{confirmationLink}"" style=""color: {PrimaryColor}; word-break: break-all;"">{confirmationLink}</a>
</p>";

        return WrapInTemplate("Confirm Your Email", body);
    }

    /// <summary>
    /// Creates a password reset email body.
    /// </summary>
    public static string CreatePasswordResetEmail(string resetLink)
    {
        var body = $@"
{CreateParagraph("We received a request to reset your password. Click the button below to create a new password.")}
{CreateButton("Reset Password", resetLink)}
{CreateWarningBox("This link will expire in 24 hours. If you didn't request a password reset, please ignore this email or contact support if you have concerns.")}
<p style=""margin: 24px 0 0 0; color: {MutedTextColor}; font-size: 13px; text-align: center;"">
    Button not working? Copy and paste this link into your browser:<br>
    <a href=""{resetLink}"" style=""color: {PrimaryColor}; word-break: break-all;"">{resetLink}</a>
</p>";

        return WrapInTemplate("Reset Your Password", body);
    }

    /// <summary>
    /// Creates a password reset code email body.
    /// </summary>
    public static string CreatePasswordResetCodeEmail(string resetCode)
    {
        var body = $@"
{CreateParagraph("We received a request to reset your password. Use the code below to complete the process.")}
<div style=""text-align: center; margin: 32px 0;"">
    <div style=""display: inline-block; background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%); border: 2px dashed {PrimaryColor}; border-radius: 12px; padding: 24px 48px;"">
        <span style=""font-family: 'SF Mono', Monaco, 'Courier New', monospace; font-size: 32px; font-weight: 700; letter-spacing: 4px; color: {PrimaryColor};"">{resetCode}</span>
    </div>
</div>
{CreateWarningBox("This code will expire in 15 minutes. If you didn't request a password reset, please ignore this email.")}";

        return WrapInTemplate("Your Password Reset Code", body);
    }
}
