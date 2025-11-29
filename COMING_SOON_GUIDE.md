# Coming Soon Access Control

This feature allows you to restrict access to your website to only yourself (or a specific admin email) while showing a "Coming Soon" page to all other visitors.

## How It Works

When enabled, the Coming Soon middleware:
1. Intercepts all requests to your website
2. Allows access to essential routes (login, static assets, health checks)
3. Checks if the authenticated user's email matches the configured admin email
4. Redirects all other users to the Coming Soon page

## Configuration

The feature is controlled by two settings in your `appsettings.json` or `appsettings.Production.json`:

```json
"ComingSoon": {
  "Enabled": false,
  "AllowedEmail": "your-email@example.com"
}
```

### Settings Explained

- **`Enabled`**: Set to `true` to activate Coming Soon mode, `false` to disable it
- **`AllowedEmail`**: The email address of the user who should have full access to the site

## How to Enable

### Step 1: Update Configuration

Edit your `appsettings.Production.json` file (or the appropriate environment config):

```json
"ComingSoon": {
  "Enabled": true,
  "AllowedEmail": "your-actual-email@example.com"
}
```

Replace `"your-actual-email@example.com"` with your actual email address that you use to log in.

### Step 2: Restart the Application

After changing the configuration, restart your application for the changes to take effect.

### Step 3: Test Access

1. **As an Admin**: 
   - Go to `/Account/Login`
   - Log in with the email specified in `AllowedEmail`
   - You should have full access to all pages

2. **As Any Other User**:
   - Visiting the site will redirect to `/comingsoon`
   - They will see the Coming Soon page
   - They can still access the login page but will be redirected after login if their email doesn't match

## How to Disable

To disable Coming Soon mode and allow all users access:

1. Edit your configuration file
2. Set `"Enabled": false`
3. Restart the application

```json
"ComingSoon": {
  "Enabled": false,
  "AllowedEmail": "your-email@example.com"
}
```

## Bypassed Routes

The following routes are always accessible even in Coming Soon mode:
- `/comingsoon` - The Coming Soon page itself
- `/Account/Login` - Login page
- `/Account/Logout` - Logout endpoint
- `/health` - Health check endpoint
- `/_framework/*` - Blazor framework files
- `/_content/*` - Static content
- `/css/*`, `/js/*`, `/images/*` - Static assets
- `/favicon.ico` - Favicon

## Customization

### Customizing the Coming Soon Page

Edit `CRS/Components/Pages/ComingSoon.razor` to customize:
- Logo
- Colors
- Text content
- Contact information
- Layout

### Multiple Allowed Emails

If you need to allow multiple admin emails, you can modify the middleware to accept a comma-separated list:

1. Update `appsettings.json`:
```json
"ComingSoon": {
  "Enabled": true,
  "AllowedEmails": "admin1@example.com,admin2@example.com"
}
```

2. Update `ComingSoonMiddleware.cs` to parse multiple emails and check against the list.

## Troubleshooting

### I'm logged in but still see the Coming Soon page

- Verify your email in the configuration exactly matches your login email
- Check that `"Enabled": true` is set correctly
- Make sure you've restarted the application after configuration changes
- Check the logs for debug messages about Coming Soon middleware

### Coming Soon page doesn't show

- Verify `"Enabled": true` is set in the correct configuration file for your environment
- Check that the middleware is registered in `Program.cs` (should be between `UseSecurityHeaders()` and `TenantResolverMiddleware`)

### Static assets aren't loading on Coming Soon page

- Ensure your static file paths are included in the `ShouldBypassComingSoon` method
- Check browser console for any 404 errors

## Security Notes

- The Coming Soon page is publicly accessible (no authentication required)
- The admin email is checked case-insensitively
- Static assets remain accessible to ensure the Coming Soon page displays correctly
- The feature works before authentication/authorization middleware, so it doesn't interfere with Identity

## Production Deployment

For production deployment:

1. Set configuration in `appsettings.Production.json` or environment variables
2. Consider using environment variables for sensitive settings:
   ```
   ComingSoon__Enabled=true
   ComingSoon__AllowedEmail=your-email@example.com
   ```
3. Remember to disable Coming Soon mode when ready to launch publicly
