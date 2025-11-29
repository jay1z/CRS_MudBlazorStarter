# Demo Environment Implementation Guide

## ⚠️ **Status: PARTIAL IMPLEMENTATION**

The demo environment requires significant database changes and authentication integration. This guide provides all the code and step-by-step instructions to complete the implementation.

---

## 📦 **What's Already Created**

### ✅ **Models**
- `CRS/Models/Demo/DemoSession.cs` - Demo session tracking
- Added `IsDemo` properties to:
  - `ApplicationUser` 
  - `Tenant`
  - `Community`
  - `ReserveStudy`
  - `ReserveStudyCommonElement`

### ✅ **Services**
- `CRS/Services/Demo/DemoAccountService.cs` - Demo account management
- `CRS/Services/Demo/DemoDataSeedService.cs` - Sample data generation

### ✅ **Components**
- `CRS/Components/Shared/DemoModeBanner.razor` - Session expiration banner

### ✅ **Database Context**
- Added `DbSet<DemoSession>` to `ApplicationDbContext`

---

## 🔧 **What Needs to Be Done**

### **Step 1: Create Database Migration**

Run these commands in Package Manager Console:

```powershell
Add-Migration AddDemoEnvironmentSupport
Update-Database
```

This will create tables and columns for:
- `DemoSessions` table
- `IsDemo` columns on Users, Tenants, Communities, Reserve Studies
- Additional demo data fields

**Expected Migration Changes**:
- New table: `DemoSessions`
- New columns on existing tables:
  - `AspNetUsers.IsDemo` (bit)
  - `Tenants.IsDemo`, `Tenants.DateDeleted`, `Tenants.OwnerId` (bit, datetime2, nvarchar)
  - `Communities.IsDemo`, `Address1`, `City`, `State`, `ZipCode`, `PhoneNumber`, `Email`, `NumberOfUnits`, `YearBuilt`, `Description`
  - `ReserveStudies.IsDemo`, `Name`, `StudyDate`, `FiscalYearEnd`, `CurrentReserveFunds`, etc.
  - `ReserveStudyCommonElements.ElementName`, `Description`, `Quantity`, `Unit`, etc.

---

### **Step 2: Register Services in Program.cs**

Add to your `Program.cs` or `Startup.cs`:

```csharp
using CRS.Services.Demo;

// ... existing code ...

// Register demo services
builder.Services.AddScoped<IDemoAccountService, DemoAccountService>();
builder.Services.AddScoped<IDemoDataSeedService, DemoDataSeedService>();
```

---

### **Step 3: Create Demo Entry Point Page**

Create `CRS/Components/Pages/Demo.razor`:

```razor
@page "/demo"
@using CRS.Services.Demo
@inject IDemoAccountService DemoService
@inject NavigationManager Nav
@inject IHttpContextAccessor HttpContextAccessor
@inject SignInManager<ApplicationUser> SignInManager

<PageTitle>Try Demo - ALX Reserve Cloud</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-8 mb-16">
    @if (isCreating)
    {
        <MudPaper Class="pa-8" Elevation="3" Style="text-align: center; border-radius: 20px;">
            <MudProgressCircular Color="Color.Primary" Size="Size.Large" Indeterminate="true" Class="mb-4" />
            <MudText Typo="Typo.h5" Class="fw-bold mb-2">
                Creating Your Demo Account...
            </MudText>
            <MudText Typo="Typo.body1" Style="color: #666;">
                Setting up sample data. This will take just a moment.
            </MudText>
        </MudPaper>
    }
    else if (!string.IsNullOrEmpty(errorMessage))
    {
        <MudAlert Severity="Severity.Error" Class="mb-4">
            @errorMessage
        </MudAlert>
        <div class="d-flex justify-center">
            <MudButton Variant="Variant.Filled" Color="Color.Primary" Href="/marketing">
                Back to Home
            </MudButton>
        </div>
    }
    else
    {
        <MudPaper Class="pa-8" Elevation="3" Style="border-radius: 20px;">
            <div style="text-align: center;">
                <MudIcon Icon="@Icons.Material.Filled.Science" 
                         Style="color: #FF8C00; font-size: 64px;" 
                         Class="mb-4" />
                <MudText Typo="Typo.h3" Class="fw-bold mb-3">
                    Try ALX Reserve Cloud Free
                </MudText>
                <MudText Typo="Typo.h6" Style="color: #666;" Class="mb-6">
                    Explore a fully functional demo with sample data. No signup required.
                </MudText>
            </div>
            
            <MudDivider Class="my-6" />
            
            <MudText Typo="Typo.subtitle1" Class="fw-bold mb-3">
                What's Included in Your Demo:
            </MudText>
            
            <MudList Dense="false">
                <MudListItem T="string" Icon="@Icons.Material.Filled.Home" IconColor="Color.Primary">
                    <MudText>3 sample properties with complete data</MudText>
                </MudListItem>
                <MudListItem T="string" Icon="@Icons.Material.Filled.Description" IconColor="Color.Primary">
                    <MudText>Pre-built reserve studies with 10+ components</MudText>
                </MudListItem>
                <MudListItem T="string" Icon="@Icons.Material.Filled.Calculate" IconColor="Color.Primary">
                    <MudText>Automated calculations and 30-year projections</MudText>
                </MudListItem>
                <MudListItem T="string" Icon="@Icons.Material.Filled.Edit" IconColor="Color.Primary">
                    <MudText>Full editing capabilities - try all features</MudText>
                </MudListItem>
                <MudListItem T="string" Icon="@Icons.Material.Filled.AccessTime" IconColor="Color.Primary">
                    <MudText>24-hour access OR 2 hours of inactivity</MudText>
                </MudListItem>
            </MudList>
            
            <MudDivider Class="my-6" />
            
            <MudText Typo="Typo.subtitle1" Class="fw-bold mb-3">
                Demo Limitations:
            </MudText>
            
            <MudList Dense="false">
                <MudListItem T="string" Icon="@Icons.Material.Filled.Block" IconColor="Color.Default">
                    <MudText Style="color: #666;">No email or export functionality</MudText>
                </MudListItem>
                <MudListItem T="string" Icon="@Icons.Material.Filled.Block" IconColor="Color.Default">
                    <MudText Style="color: #666;">Changes won't be saved after session expires</MudText>
                </MudListItem>
            </MudList>
            
            <div class="d-flex justify-center mt-6">
                <MudButton Variant="Variant.Filled" 
                           Color="Color.Warning" 
                           Size="Size.Large"
                           StartIcon="@Icons.Material.Filled.PlayArrow"
                           OnClick="CreateDemoSession"
                           Disabled="@isCreating"
                           Style="text-transform: none; font-weight: 600; padding: 16px 32px;">
                    Start Demo Now (Instant Access)
                </MudButton>
            </div>
            
            <MudText Typo="Typo.caption" Align="Align.Center" Class="mt-4" Style="color: #999;">
                No credit card required • No personal information needed
            </MudText>
        </MudPaper>
    }
</MudContainer>

@code {
    private bool isCreating = false;
    private string? errorMessage = null;
    
    private async Task CreateDemoSession()
    {
        isCreating = true;
        errorMessage = null;
        
        try
        {
            // Get IP address and user agent
            var httpContext = HttpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
            var referrer = httpContext?.Request.Headers["Referer"].ToString();
            
            // Create demo session
            var session = await DemoService.CreateDemoSessionAsync(ipAddress, userAgent, referrer);
            
            // Sign in the demo user
            var user = await UserManager.FindByIdAsync(session.DemoUserId);
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false);
                
                // Redirect to tenant dashboard with session ID
                Nav.NavigateTo($"/tenant?demo={session.SessionId}", forceLoad: true);
            }
            else
            {
                errorMessage = "Failed to sign in to demo account. Please try again.";
            }
        }
        catch (InvalidOperationException ex)
        {
            errorMessage = ex.Message; // Rate limit message
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred creating your demo session. Please try again.";
            Console.WriteLine($"Demo creation error: {ex.Message}");
        }
        finally
        {
            isCreating = false;
        }
    }
    
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
}
```

---

### **Step 4: Add Demo Link to Marketing Page**

In `CRS/Components/Pages/MarketingHome.razor`, add demo CTA:

```razor
<!-- In hero section, alongside "Start Free Trial" button -->
<MudButton Variant="Variant.Outlined" 
           Color="Color.Primary" 
           Size="Size.Large"
           StartIcon="@Icons.Material.Filled.Science"
           Href="/demo"
           Style="text-transform: none; font-weight: 600;">
    Try Live Demo
</MudButton>
```

Also add to sticky navigation:

```razor
<MudButton Variant="Variant.Text" Color="Color.Primary" Href="/demo" Style="text-transform: none;">
    Live Demo
</MudButton>
```

---

### **Step 5: Create Background Cleanup Service**

Create `CRS/Services/Demo/DemoCleanupService.cs`:

```csharp
using CRS.Services.Demo;

namespace CRS.Services.Background
{
    public class DemoCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DemoCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
        
        public DemoCleanupService(
            IServiceProvider services,
            ILogger<DemoCleanupService> logger)
        {
            _services = services;
            _logger = logger;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Demo Cleanup Service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                    
                    using (var scope = _services.CreateScope())
                    {
                        var demoService = scope.ServiceProvider.GetRequiredService<IDemoAccountService>();
                        var count = await demoService.CleanupExpiredSessionsAsync();
                        
                        if (count > 0)
                        {
                            _logger.LogInformation("Cleaned up {Count} expired demo sessions", count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in demo cleanup service");
                }
            }
            
            _logger.LogInformation("Demo Cleanup Service stopped");
        }
    }
}
```

Register in `Program.cs`:

```csharp
builder.Services.AddHostedService<DemoCleanupService>();
```

---

### **Step 6: Add Demo Banner to Tenant Layout**

In your tenant layout file (e.g., `_TenantLayout.razor` or `MainLayout.razor`), add:

```razor
@using CRS.Components.Shared
@inject AuthenticationStateProvider AuthStateProvider
@inject CRS.Services.Demo.IDemoAccountService DemoService

<!-- At the top of your layout, before navigation -->
@if (isDemoMode)
{
    <DemoModeBanner SessionId="@sessionId" />
}

@code {
    private bool isDemoMode = false;
    private string? sessionId = null;
    
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            isDemoMode = await DemoService.IsDemoModeAsync(userId);
            
            if (isDemoMode)
            {
                // Get session ID from query string or user claims
                sessionId = // TODO: Get from context
            }
        }
    }
}
```

---

### **Step 7: Disable Export/Email Features in Demo Mode**

Add checks in your export and email services:

```csharp
public class ReportExportService
{
    private readonly IDemoAccountService _demoService;
    
    public async Task<byte[]> ExportToPdfAsync(Guid reportId, string? userId)
    {
        // Check if demo mode
        if (await _demoService.IsDemoModeAsync(userId))
        {
            throw new InvalidOperationException("Export functionality is not available in demo mode. Create a free account to export reports.");
        }
        
        // ... existing export logic
    }
}
```

Show upgrade prompts in UI:

```razor
@if (isDemoMode)
{
    <MudAlert Severity="Severity.Info" Class="mb-4">
        <MudText>
            <MudIcon Icon="@Icons.Material.Filled.Info" Size="Size.Small" />
            <strong>Demo Mode:</strong> Export functionality is disabled. 
            <MudButton Color="Color.Primary" Size="Size.Small" Href="/tenant/signup?from=demo">
                Create Free Account
            </MudButton>
            to enable all features.
        </MudText>
    </MudAlert>
}
```

---

### **Step 8: Update Tenant Signup to Handle Demo Conversion**

In `SignupPage.razor` or similar:

```razor
@code {
    [Parameter]
    [SupplyParameterFromQuery(Name = "from")]
    public string? Source { get; set; }
    
    [Parameter]
    [SupplyParameterFromQuery(Name = "session")]
    public string? DemoSessionId { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        if (Source == "demo" && !string.IsNullOrEmpty(DemoSessionId))
        {
            // Pre-fill form, show special message
            showDemoConversionMessage = true;
        }
    }
    
    private async Task HandleSignup()
    {
        if (!string.IsNullOrEmpty(DemoSessionId))
        {
            // Convert demo to real account
            var success = await DemoService.ConvertToRealAccountAsync(
                DemoSessionId, 
                email, 
                password
            );
            
            if (success)
            {
                // Show success message
                Snackbar.Add("Demo account converted! Your data has been preserved.", Severity.Success);
                // ... continue with normal signup flow
            }
        }
        else
        {
            // Normal signup
        }
    }
}
```

---

## 🔄 **Activity Tracking**

To track user activity and reset inactivity timer, add middleware:

```csharp
public class DemoActivityMiddleware
{
    private readonly RequestDelegate _next;
    
    public DemoActivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, IDemoAccountService demoService)
    {
        // Check if user is in demo mode
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (await demoService.IsDemoModeAsync(userId))
            {
                // Get session ID from claims or context
                var sessionId = context.Request.Query["demo"].ToString();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await demoService.UpdateLastActivityAsync(sessionId);
                }
            }
        }
        
        await _next(context);
    }
}
```

Register in `Program.cs`:

```csharp
app.UseMiddleware<DemoActivityMiddleware>();
```

---

## 🧪 **Testing the Demo Environment**

### Test Flow:
1. Navigate to `/demo`
2. Click "Start Demo Now"
3. Verify demo session created in database
4. Verify sample data seeded (3 properties, reserve studies, elements)
5. Verify demo banner shows at top
6. Try to export - should show error/upgrade prompt
7. Try to send email - should show error/upgrade prompt
8. Navigate around - verify activity tracking
9. Wait for expiration or close browser
10. Verify cleanup service removes expired sessions

---

## 📊 **Demo Analytics to Track**

Using Microsoft Clarity (already installed), track:
- Demo session creation rate
- Average session duration
- Features used in demo
- Drop-off points
- Demo→trial conversion rate
- Most viewed pages in demo

Add custom events:

```javascript
// In your demo tracking
clarity("event", "demo_started");
clarity("event", "demo_feature_used", "reserve_study_created");
clarity("event", "demo_converted");
```

---

## 🚀 **Go-Live Checklist**

Before enabling demo environment in production:

- [ ] Database migration complete
- [ ] Services registered in DI container
- [ ] Demo entry page created and tested
- [ ] Demo banner component working
- [ ] Background cleanup service running
- [ ] Export/email features properly restricted
- [ ] Activity tracking middleware added
- [ ] Demo→real account conversion tested
- [ ] Rate limiting tested (try creating 4 demos from same IP)
- [ ] Session expiration tested (change timeout to 5 minutes for testing)
- [ ] Mobile responsiveness verified
- [ ] Analytics tracking configured
- [ ] Monitor logs for first week

---

## 🔧 **Configuration Options**

Add to `appsettings.json`:

```json
{
  "DemoEnvironment": {
    "Enabled": true,
    "SessionExpirationHours": 24,
    "InactivityTimeoutHours": 2,
    "MaxSessionsPerIp": 3,
    "RateLimitWindowHours": 24,
    "CleanupIntervalHours": 1
  }
}
```

---

## 💡 **Tips & Best Practices**

### Security
- Never log demo user passwords
- Clean up demo data regularly
- Monitor for abuse (same IP creating many demos)
- Consider CAPTCHA if abuse detected

### Performance
- Index demo-related columns for queries
- Run cleanup during low-traffic hours
- Monitor database growth from demo data

### User Experience
- Make demo banner prominent but not annoying
- Show countdown when < 1 hour remains
- Offer to save work before expiration
- Make conversion to real account seamless

### Data Quality
- Keep sample data realistic and up-to-date
- Include variety of property sizes
- Add comments/notes to sample data explaining features
- Update when you add new features

---

## 🐛 **Troubleshooting**

### Demo Creation Fails
- Check database connection
- Verify migrations ran successfully
- Check service registration
- Review logs for specific error

### Session Not Expiring
- Verify background service is running
- Check cleanup interval configuration
- Manually run cleanup: `await demoService.CleanupExpiredSessionsAsync();`

### Rate Limiting Not Working
- Verify IP address capture is correct (check for proxy/load balancer)
- Test with different IP addresses
- Check `IsRateLimitedAsync` logic

### Data Not Seeding
- Check foreign key constraints
- Verify tenant ID is set correctly
- Review seed service logs
- Test seed service independently

---

## 📈 **Expected Results**

Once fully implemented:

- **Demo Sessions**: 50-100 per week (depends on traffic)
- **Conversion Rate**: 10-15% demo→trial
- **Average Duration**: 15-20 minutes
- **Most Popular Features**: Reserve study creation, calculations
- **Bounce Rate**: -30% (visitors engage more with demo)

---

## ✅ **Implementation Summary**

**Time Required**: 2-4 hours (with testing)

**Steps**:
1. Run database migration (5 min)
2. Register services (2 min)
3. Create demo entry page (30 min)
4. Add demo links to marketing page (10 min)
5. Create background cleanup service (20 min)
6. Add demo banner to layout (15 min)
7. Restrict export/email features (30 min)
8. Update signup for conversion (30 min)
9. Add activity tracking (20 min)
10. Test thoroughly (60 min)

**Priority Order**:
1. Database migration (REQUIRED)
2. Service registration (REQUIRED)
3. Demo entry page (REQUIRED)
4. Demo banner (REQUIRED)
5. Background cleanup (REQUIRED)
6. Feature restrictions (HIGH)
7. Activity tracking (MEDIUM)
8. Conversion flow (MEDIUM)
9. Analytics (LOW)

---

**Ready to implement?** Start with Step 1 (database migration) and work through sequentially. Test each step before moving to the next.

**Questions?** Review the code in `/Services/Demo/` and `/Models/Demo/` for implementation details.

**Need help?** All services and components are already created - you just need to wire them up!
