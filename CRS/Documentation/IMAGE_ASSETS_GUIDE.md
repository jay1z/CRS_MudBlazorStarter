# Marketing Page - Image Assets Guide

## 🖼️ Required Image Update

### Hero Section Image
**File Path**: `CRS/wwwroot/images/dashboard-preview.png`

**Current Status**: ⚠️ Currently using logo.png as placeholder

**Required**: Replace with actual dashboard screenshot

#### Recommended Specifications
- **Dimensions**: 1200x800px (3:2 aspect ratio)
- **Format**: PNG with transparency OR JPG
- **File Size**: < 500KB (optimized)
- **Content**: Screenshot of your main dashboard showing:
  - Property overview
  - Key metrics/statistics
  - Charts or graphs
  - Clean, professional UI

#### How to Create
1. **Option 1 - Screenshot**:
   - Navigate to your dashboard
   - Use a browser extension like "Full Page Screen Capture"
   - Crop to show the best features
   - Use tools like TinyPNG to optimize

2. **Option 2 - Mockup**:
   - Use Figma, Sketch, or Adobe XD
   - Create a polished mockup with sample data
   - Export as PNG at 2x resolution

3. **Option 3 - Professional**:
   - Hire a designer on Fiverr or Upwork
   - Cost: $50-$200
   - Turnaround: 1-3 days

#### Placeholder Alternative
If you don't have a dashboard screenshot yet, you can:
1. Use a generic dashboard mockup from:
   - Unsplash (search "dashboard mockup")
   - UI8 or Creative Market (paid templates)
2. Temporarily keep logo.png until ready

---

## 📸 Image Optimization Checklist

### Current Images
- ✅ `logo.png` - Already exists
- ✅ `login_2.png` - Already exists
- ⚠️ `dashboard-preview.png` - Needs to be added

### Optimization Steps
1. **Resize**: Use appropriate dimensions (not too large)
2. **Compress**: Use TinyPNG or similar (aim for <500KB)
3. **Format**: 
   - PNG for logos/icons (transparency needed)
   - JPG for photos/screenshots (smaller file size)
   - WebP for modern browsers (best compression)
4. **Lazy Loading**: Consider adding `loading="lazy"` for below-fold images

---

## 🎨 Brand Assets Location

All brand assets should be stored in: `CRS/wwwroot/images/`

### Current Assets
```
images/
├── alx_reserve_fav_icon.png    (Favicon)
├── headshot.jpg                 (Team photo?)
├── icon.png                     (App icon)
├── login_1.png                  (Login page image)
├── login_2.png                  (Login page image - currently used)
├── login.png                    (Login page image)
└── logo.png                     (Main logo)
```

### Recommended Additions
```
images/
├── dashboard-preview.png        ⚠️ HIGH PRIORITY
├── feature-screenshot-1.png     (Optional)
├── feature-screenshot-2.png     (Optional)
├── customer-logo-1.png          (Future)
├── customer-logo-2.png          (Future)
└── testimonial-avatar-1.png     (Future)
```

---

## 🔄 Quick Image Replacement Guide

### Step 1: Add New Image
```bash
# Copy your image to the images folder
Copy-Item "path/to/your/dashboard-screenshot.png" "CRS/wwwroot/images/dashboard-preview.png"
```

### Step 2: Verify in Browser
1. Run the application
2. Navigate to the marketing home page
3. Check that the image loads correctly
4. Verify it looks good on mobile devices

### Step 3: Optimize if Needed
If the image is too large (>500KB):
- Use https://tinypng.com/
- Or use PowerShell:
```powershell
# Install ImageMagick first
magick convert dashboard-preview.png -quality 85 -resize 1200x800 dashboard-preview-optimized.png
```

---

## 📱 Responsive Image Best Practices

### Current Implementation
The hero image is wrapped in a responsive container:
```razor
<MudImage Src="images/dashboard-preview.png" 
          Alt="ALX Reserve Cloud Dashboard Preview" 
          Class="rounded-lg"
          Style="width: 100%; border-radius: 16px; display: block;" 
          ObjectFit="ObjectFit.Cover"/>
```

### Potential Enhancement (Future)
Use `<picture>` element for multiple resolutions:
```html
<picture>
  <source media="(max-width: 600px)" srcset="images/dashboard-preview-mobile.png">
  <source media="(min-width: 601px)" srcset="images/dashboard-preview-desktop.png">
  <img src="images/dashboard-preview.png" alt="Dashboard" style="width:100%">
</picture>
```

---

## 🎯 Image Guidelines

### DO:
- ✅ Use high-quality, professional screenshots
- ✅ Show actual product features
- ✅ Keep file sizes under 500KB
- ✅ Use descriptive alt text for accessibility
- ✅ Test on mobile devices

### DON'T:
- ❌ Use stock photos that don't represent your product
- ❌ Upload unoptimized 5MB+ images
- ❌ Forget alt text (hurts SEO and accessibility)
- ❌ Use pixelated or low-quality images
- ❌ Include sensitive customer data in screenshots

---

## 🚀 Quick Win

**Temporary Solution**: Until you have a real dashboard screenshot, you can:
1. Keep the current logo.png
2. Add a subtle badge overlay: "Live Dashboard Preview Coming Soon"
3. Or use a clean mockup from free resources like:
   - https://www.figma.com/community (search "dashboard")
   - https://www.uplabs.com/search?q=dashboard
   - https://dribbble.com/search/dashboard (inspiration)

---

**Need Help?**
- Ping the design team for screenshots
- Use browser DevTools to take clean screenshots (hide scrollbars, etc.)
- Consider hiring a freelancer on Fiverr for $20-50 to create a polished mockup

---

**Last Updated**: December 2024
