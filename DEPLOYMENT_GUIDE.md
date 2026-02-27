# Deployment Guide - After DI Fix

## Pre-Deployment Checklist

Before deploying to production, verify:

- [x] Build successful locally
- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] Application runs successfully in local published mode
- [ ] Database migrations are ready

## Testing Locally in Published Mode

1. **Publish the application:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Run the published application:**
   ```bash
   cd publish
   dotnet Horizon.dll
   ```

3. **Verify these scenarios:**
   - Application starts without errors
   - Can log in successfully
   - Tenant isolation works (users see only their tenant's data)
   - Audit logs are created for data changes
   - Database seeding completes if running fresh
   - All pages load correctly

## Deployment Steps

### Option 1: Azure App Service

1. **Update your publish profile** (if needed)
2. **Right-click project** → Publish
3. **Select target**: Azure App Service
4. **Deploy**

### Option 2: IIS

1. **Publish to folder:**
   ```bash
   dotnet publish -c Release -o C:\inetpub\wwwroot\Horizon
   ```

2. **Configure IIS:**
   - Create Application Pool (.NET CLR Version: No Managed Code)
   - Create Website/Application pointing to publish folder
   - Ensure proper permissions for App Pool identity

3. **Verify web.config** has ASP.NET Core Module:
   ```xml
   <aspNetCore processPath="dotnet" arguments=".\Horizon.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
   ```

### Option 3: Docker

1. **Build Docker image:**
   ```bash
   docker build -t horizon-app .
   ```

2. **Run container:**
   ```bash
   docker run -d -p 8080:8080 --name horizon-app \
     -e ConnectionStrings__DefaultConnection="your-connection-string" \
     horizon-app
   ```

## Post-Deployment Verification

### 1. Check Application Logs
Look for these startup messages:
```
[Seed] Migrating database...
[Seed] Seeding roles...
[Seed] Ensuring platform tenant...
[Seed] Seeding platform admin user...
[Seed] Completed.
[Startup] App:RootDomain set to 'yourdomain.com'
```

### 2. Test Critical Paths
- [ ] Homepage loads
- [ ] User can log in
- [ ] User can create/edit data
- [ ] Tenant isolation verified (test with multiple tenants)
- [ ] Audit logs are being created
- [ ] Email notifications work (if configured)

### 3. Monitor for Issues
Watch for these potential issues:
- **High memory usage**: Check if context disposal is working
- **Slow queries**: Verify tenant filters are applied
- **Authentication errors**: Check cookie configuration
- **Database connection errors**: Verify connection string

## Environment-Specific Configuration

### Production appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_PRODUCTION_CONNECTION_STRING"
  },
  "App": {
    "RootDomain": "yourdomain.com",
    "ShareAuthAcrossSubdomains": false
  },
  "Identity": {
    "RequireConfirmedAccount": true
  }
}
```

### Key Settings to Review:
- **ConnectionStrings**: Point to production database
- **App:RootDomain**: Set your production domain
- **Identity:RequireConfirmedAccount**: Enable for production
- **Serilog**: Configure proper logging (Application Insights, etc.)
- **Stripe**: Use production keys
- **AzureStorage**: Use production storage account

## Rollback Plan

If issues occur after deployment:

1. **Immediate Rollback:**
   - Revert to previous deployment
   - Or restore previous publish folder

2. **Database Rollback:**
   - If migrations were run, may need to rollback migrations
   - Check for data corruption

3. **Monitor:**
   - Check application logs
   - Review error messages
   - Identify root cause

## Performance Tuning

After deployment, monitor these metrics:

1. **Response Times:**
   - Homepage: < 500ms
   - Data pages: < 1s
   - API calls: < 300ms

2. **Database:**
   - Connection pool usage
   - Query performance
   - Index effectiveness

3. **Memory:**
   - Watch for memory leaks
   - Context disposal issues
   - Cache effectiveness

## Common Issues and Solutions

### Issue: "Cannot resolve scoped service" error returns
**Solution:** 
- Verify the custom factory is registered
- Check that `ScopedDbContextFactory` class exists
- Ensure `ApplicationDbContext` constructor parameters are optional

### Issue: Tenant isolation not working
**Solution:**
- Check `ITenantContext` is being set in middleware
- Verify tenant query filters are applied
- Check `TenantResolverMiddleware` is registered

### Issue: Slow queries
**Solution:**
- Check indexes on `TenantId` columns
- Review query execution plans
- Consider caching frequently accessed data

### Issue: Memory leaks
**Solution:**
- Verify contexts are being disposed
- Check factory usage in singleton services
- Review circuit handler cleanup

## Security Checklist

Before going live:

- [ ] HTTPS enforced
- [ ] Strong password policy enabled
- [ ] SQL injection prevention verified (using EF parameterized queries)
- [ ] XSS protection enabled (CSP headers)
- [ ] CSRF protection enabled (antiforgery tokens)
- [ ] Tenant isolation tested thoroughly
- [ ] Audit logging verified
- [ ] Secure cookies configured
- [ ] Rate limiting considered
- [ ] Error messages don't leak sensitive info

## Support Resources

If you encounter issues:

1. **Check logs:** Application logs will show startup errors
2. **Review error details:** Enable detailed errors in development
3. **Test locally:** Reproduce in local published mode
4. **Check dependencies:** Verify all NuGet packages are restored
5. **Database connectivity:** Test connection string separately

## Success Indicators

Deployment is successful when:

✅ Application starts without errors  
✅ Users can log in and access their tenant data  
✅ No "cannot resolve scoped service" errors  
✅ Tenant isolation is working  
✅ Audit logs are being created  
✅ Performance is acceptable  
✅ No memory leaks detected  

## Next Steps After Successful Deployment

1. **Monitor for 24 hours**: Watch logs and metrics
2. **Gather user feedback**: Check for any issues
3. **Performance review**: Analyze bottlenecks
4. **Optimize as needed**: Address any problems
5. **Document any issues**: Update runbook

---

**Last Updated:** After DI fix implementation  
**Build Status:** ✅ Successful  
**Ready for Deployment:** ✅ Yes
