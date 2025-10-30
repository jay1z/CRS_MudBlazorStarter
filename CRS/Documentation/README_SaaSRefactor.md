Prompt:

I’m refactoring this Blazor Server application into a multi-tenant SaaS platform.
The project is currently structured for single-tenant, self-hosted use, but I need to reorganize the solution, introduce tenant isolation, and prepare for cloud deployment.

Please carefully review the existing structure and propose or apply changes that align with the following requirements.

🎯 Goal

Transform the existing Blazor Server project into a SaaS-ready architecture that supports:

- Multiple tenants (companies) sharing a single deployment
- Tenant-aware authentication, authorization, and data access
- Cloud file storage with per-tenant organization
- Clean folder structure for scalability and maintainability
- Centralized updates (one app instance serving all customers)

---

This document tracks the SaaS refactor plan and also documents recent editor work added as part of the Block/Visual editor feature set (inspired by Divi UX). It contains the high-level refactor guidance and concrete implementation notes for the editor scaffolding that was added to the repository.

Contents
- Refactor plan (summary)
- Multi-tenant foundation
- Data access notes
- Authentication & identity
- File & storage
- Branding and tenant layout
- Visual page editor (new)
 - What was added
 - File locations
 - Block model changes
 - How to persist/load editor state
 - Recommended next steps for editor
- Migration & deployment notes

---

1) Refactor plan (summary)
- Keep Blazor Server app as the host and introduce tenant-awareness via an `ITenantContext` and `TenantResolverMiddleware`.
- Scope data by `TenantId` using EF Core query filters and inject `ITenantContext` into the DbContext.
- Store per-tenant assets under `wwwroot/tenant-assets/tenant-{id}/` or in blob storage with tenant prefixes.
- Provide a tenant-aware admin UI (Tenants page exists) and an admin block-based homepage editor.

2) Multi-tenant foundation
- Tenant entity exists in the data model (`CRS.Models.Tenant`) with `Id, Name, Subdomain, IsActive, BrandingJson, CreatedAt`.
- `ITenantContext` and `TenantContext` are implemented and used across layouts and services.
- `TenantResolverMiddleware` resolves tenants by host subdomain and now prefers an authenticated user tenant when available.
- UI flows (Admin → Tenants) provide a manual select feature to set `TenantContext` for the current UI session and persist it to user settings/localStorage.

3) Data access notes
- Add `TenantId` to all tenant-scoped entities (already applied in many models). Use EF Core query filters in `ApplicationDbContext.OnModelCreating` to limit data to the current tenant.
- Inject `ITenantContext` into `ApplicationDbContext` so the filter can use the current request's tenant id.
- When creating new tenant-scoped entities, set `TenantId = _tenantContext.TenantId` before saving.

4) Authentication & identity
- `ApplicationUser` includes `TenantId` and tenant claims are added via `TenantClaimsPrincipalFactory`.
- Consider validating that authenticated user's TenantId aligns with middleware-resolved tenant during login; current middleware prefers the user tenant when present.

5) File & storage
- Use the per-tenant directory convention under `wwwroot/tenant-assets/tenant-{id}/` for local development.
- For production, prefer Azure Blob storage or another object store and prefix paths by tenant id.
- Add a media upload API (authenticated + tenant-scoped) and a media library UI for re-use in blocks.

6) Branding and tenant layout
- `_TenantLayout.razor` applies `ThemeService` branding from `TenantContext.BrandingJson`.
- ThemeService supports presets and merging branding payloads for per-tenant themeing.

---

Visual page editor (new) — Overview

The repository now includes a scaffolded Visual Page Editor (Divi-inspired UX but implemented natively in Blazor), implemented as components that render a nested block model and provide inspector / palette UI.

Goal
- Provide a block-based visual editor for tenant homepages.
- Persist editor state as JSON into `TenantHomepage.DraftJson` and render sanitized HTML in `DraftHtml`/`PublishedHtml` for preview/publish flows.

What was added
- A minimal, extensible editor scaffold was created under `CRS/Components/Editors/`:
 - `_Imports.razor` (editor namespace imports & alias)
 - `Models/EditorModels.cs` (EditorPageModel & Block model)
 - `BlockPalette.razor` (sidebar to add new blocks)
 - `BlockRenderer.razor` (recursive renderer for section/row/column/module blocks)
 - `InspectorPanel.razor` (right-side inspector to edit selected block props)
 - `VisualPageEditor.razor` (main canvas which loads/saves the tenant homepage draft)
- The existing `EditBlockDialog.razor` was extended earlier to support multiple block types (text, hero, image, cta, gallery, testimonial, faq).
- The legacy `PageBlock` model was extended to support module-level fields used by these block types.
- The admin block editor `CRS/Components/Pages/Admin/HomepageEditor.razor` remains in place and includes a simplified block list view with inline previews, drag/drop reordering, visual drop indicators, and publishing.

Block model changes
- New editor `Block` model (used by the Visual editor scaffold) is a tree node with `Id`, `Type`, `Props`, `Children`.
- The legacy `CRS.Models.PageModel` / `CRS.Models.BlockModel` was extended earlier to support specific block-level fields (image url/alt/caption, CTA props, gallery images, testimonial, FAQ list). The editor scaffolding can interoperate with either representation by serializing to `TenantHomepage.DraftJson`.

How to persist & load editor state
- `VisualPageEditor` loads the tenant's `TenantHomepage.DraftJson` (if present) and deserializes into the `EditorPageModel`.
- Save flow: Serialize `EditorPageModel` back into `TenantHomepage.DraftJson` and also produce sanitized HTML for `DraftHtml` (use existing `GenerateHtmlFromBlocks` or a custom renderer). Call `SaveDraftAsync` and `PublishAsync` flows similar to the existing admin page editor.

Files created (quick reference)
- `CRS/Components/Editors/_Imports.razor`
- `CRS/Components/Editors/Models/EditorModels.cs`
- `CRS/Components/Editors/BlockPalette.razor`
- `CRS/Components/Editors/BlockRenderer.razor`
- `CRS/Components/Editors/InspectorPanel.razor`
- `CRS/Components/Editors/VisualPageEditor.razor`

Integration guidance (how to use)
1. Open `Admin → Homepage Editor` or navigate to `/Admin/HomepageEditor` to use the simple block list editor.
2. For the new visual editor scaffold, add a page or navigation link to `Components/Editors/VisualPageEditor.razor` (or embed the component in an admin page). It loads the tenant draft if available.
3. Use the palette to add a Section/Row/Column/Module, use the Inspector to edit block properties and save.
4. When ready, use the Publish action to generate sanitized HTML and store `PublishedJson`/`PublishedHtml` on `TenantHomepage`.

---

TODO (short checklist)

- [ ] Implement media upload API + per-tenant storage (POST /api/tenant/{tenantId}/media)
- [ ] Integrate image upload / media picker into Inspector (Image/Gallery blocks)
- [ ] Integrate rich text editor for Text/Hero blocks (HtmlEditor)
- [ ] Add autosave (debounced) and visible save indicator in the editor toolbar
- [ ] Implement undo/redo stack (Ctrl+Z / Ctrl+Y) and UI buttons
- [ ] Improve drag/drop UX with top/bottom insertion indicators and animations
- [ ] Add templates/presets save & load
- [ ] Add publish history and ability to unpublish
- [ ] Add unit/integration tests for TenantHomepageService and media upload
- [ ] Harden file upload validation (mime, size) and add virus scanning for production

Recommended next steps for the editor (priority)
1. Media upload API + per-tenant storage (recommended first):
 - Add a controller endpoint `POST /api/tenant/{tenantId}/media` that accepts files, stores them under `wwwroot/tenant-assets/tenant-{id}/`, and returns public URLs.
 - Integrate upload into the Image/Gallery block inspector so editors can select uploaded files.
2. Rich text editing:
 - Integrate the existing `HtmlEditor` into a `TextBlockEditor` module so editors can edit HTML for text/hero blocks.
3. Undo/redo, autosave, and debounce saves:
 - Add a small undo stack (list of JSON snapshots) and implement Ctrl+Z/Ctrl+Y or toolbar undo/redo.
 - Add autosave that triggers after a debounce (e.g.,2s after last change) and indicates save status in the UI.
4. Improve drag/drop UX:
 - Insert-line logic currently renders an insert line when dragging over a block; refine to support top/bottom drop zones and animations.
5. Templates & presets:
 - Allow saving a page or block as a reusable template and loading templates into the canvas.
6. Access control and publish logs:
 - Track who publishes (user id) and when; provide ability to unpublish and view publish history.
7. Tests & validation:
 - Add unit tests for the TenantHomepageService save/publish flows and validation for uploaded media.

Security & sanitization
- Always sanitize HTML server-side before saving `PublishedHtml` — use the existing `CRS.Services.HtmlSanitizerHelper` which uses Ganss.XSS.
- Validate uploaded images (mime type and size) and store them in tenant-scoped directories.

Migration notes
- Legacy content can be migrated into the new block model. A pragmatic approach:
 - Deserialize existing `TenantHomepage.DraftJson` if present and map legacy `BlockModel` entries into the new `Block` tree.
 - For single-tenant legacy apps create a default tenant (Id =1) and assign existing records to that tenant.

Deployment & hosting
- Continue using .NET9 (project targets), configure wildcard hosting and TLS for subdomains when using host-based tenant resolution.
- Register `TenantResolverMiddleware` early in the pipeline (before authentication) so tenant context is available to authentication/authorization.

Closing notes
- This repo now contains an initial Visual Editor scaffold that demonstrates the core ideas: nested container blocks (section/row/column), module rendering, inspector editing, and the persistence pattern to tenant homepage JSON. It is intentionally minimal and designed for incremental improvements.

If you want, I will:
- Wire the VisualPageEditor into the Admin navigation and add Save/Publish buttons that reuse the existing `TenantHomepage` persistence code.
- Implement the media upload endpoint and integrate it into the Image/Gallery inspector.
- Add autosave and a visual save indicator.

Tell me which editor feature to implement next and I will apply the changes in the workspace.

## Studio SDK — Recommended improvements and next steps

Following the recent work to embed the GrapesJS Studio SDK and the Visual Editor scaffold, add the following prioritized improvements and implementation notes to the refactor README so the team has a clear actionable plan.

1) Secure and reliable SDK delivery
- Host Studio SDK and plugins locally under `wwwroot/vendor/grapes-studio/` for deterministic load behavior during development and production.
- Add CDN fallback only after local check; include SRI and CSP headers when using unpkg/jsdelivr in production.

2) Storage & server-side handlers (required)
- Implement endpoints for assets and projects:
 - `POST /api/studio/assets/upload` — accept multipart files, store under per-tenant path `tenant-assets/tenant-{id}/`, return `[{ src: '...' }]`.
 - `DELETE /api/studio/assets` — accept asset identifiers/paths and delete tenant-scoped assets.
 - `POST /api/studio/projects/save` — persist project JSON, rendered HTML, metadata (author, tenant, notes) and return project id/url.
 - `GET /api/studio/projects/load/{id}` — return project JSON for editor load.
 - `GET /api/studio/projects/list` — return list used by the `listPages` plugin.
- Persist projects and versions with EF Core (Page / PageVersion), include TenantId, AuthorId, CreatedAt, IsPublished.

3) Authorization & tenant scoping
- Require authenticated Admin or Editor role for Studio page and API endpoints.
- Validate that the active `ITenantContext.TenantId` aligns with the project being saved/loaded; enforce tenant isolation.

4) Asset manager integration
- Wire `assets.onUpload` in the SDK initialization to call the upload endpoint and return stored URLs to the editor.
- Use `FileStorageService` abstraction (already present in the repo) to write files to local or cloud storage.
- Validate content type and file size server-side; reject or sanitize invalid uploads.

5) Publish workflow and sanitization
- When a project is published, generate sanitized HTML (using `Ganss.Xss.HtmlSanitizer`) and store it in `TenantHomepage.PublishedHtml` or `PageVersion.PublishedHtml`.
- Keep drafts editable (project JSON + DraftHtml) and only copy to PublishedHtml when explicitly published.
- Record publish metadata (user id, timestamp, version note) for audit/history.

6) Autosave, undo/redo, and UX improvements
- Implement `storage.onSave` to create a PageVersion snapshot and support autosave with a visible status indicator.
- Add a small undo/redo stack (JSON snapshots) in the editor UI with Ctrl+Z / Ctrl+Y support.
- Provide Save Draft, Publish, Revert to Version, and Export actions in the editor toolbar.

7) Custom blocks & templates
- Build domain-specific blocks (Reserve Study Listing, CTA, Hero) and register them during SDK init so editors have app-aware building blocks.
- Allow saving blocks or full pages as a reusable template stored per-tenant.

8) Preview & management UI
- Enhance `/Admin/GrapesPreview` to list PageVersions, allow preview in iframe, publish, unpublish, and load into editor.
- Add Project/Version listing UI for admin management and search/filter by tenant, author, or date.

9) Observability & security
- Log saves/publishes with Serilog including tenant and user claims.
- Sanitize and validate project JSON server-side; implement server-side checks before publishing.
- Add rate limiting or quotas for asset uploads to prevent abuse.

10) Tests & validation
- Add unit tests for TenantHomepageService, StudioController endpoints, and file upload validation.
- Add integration tests that exercise editor save/load/publish flows (using sample project JSON).

Suggested immediate tasks (priority order)
- Implement the media upload API and integrate it into the Studio `assets.onUpload` handler.
- Implement `POST /api/studio/projects/save` and persist PageVersion in the DB.
- Add role-based authorization to Studio pages and API endpoints.
- Host SDK files locally (wwwroot) and update the Blazor component to prefer local files.


Notes
- The above steps align with the multi-tenant guidance already documented in this README: tenant-scoped storage, EF filters, and `ITenantContext` usage should be used throughout the Studio integration for safe multi-tenant operations.
- Keep raw project JSON in the DB for editing; store sanitized HTML for serving publicly.

---

(Added: Studio SDK improvements and next steps — auto-generated from developer conversation.)