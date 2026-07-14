# SEO & Analytics

Open Planning Poker implements a comprehensive SEO strategy across both the marketing website and the web application.

## SEO Features

### Website (`openplanningpoker.com`)

| Feature | Implementation |
|---------|---------------|
| **Title Tag** | `Open Planning Poker — Free & Open Source Agile Estimation Tool` |
| **Meta Description** | Rich, keyword-optimized description |
| **Meta Keywords** | 9 relevant keywords |
| **Canonical URL** | `https://openplanningpoker.com/` |
| **hreflang Tags** | `en` and `x-default` |
| **Open Graph Tags** | Full set: `og:title`, `og:description`, `og:image` (512×512), `og:image:width/height/alt`, `og:type`, `og:url`, `og:site_name`, `og:locale` |
| **Twitter Card** | `summary_large_image` with `@openplanningpoker` handle |
| **Robots Meta** | `index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1` |
| **robots.txt** | Allows all crawlers, references sitemap |
| **sitemap.xml** | Lists main page and app subdomain |
| **Custom 404 Page** | Semantic, branded, with `noindex` meta tag |
| **Semantic HTML** | `<header>`, `<main>`, `<footer>`, `<nav>`, `<section>`, proper heading hierarchy (`h1` → `h2` → `h3`) |
| **Image Alt Text** | Descriptive alt attributes on all images |
| **Image Dimensions** | Explicit `width`/`height` to prevent CLS |
| **Lazy Loading** | `loading="lazy"` on below-fold images |
| **Preconnect** | Google Tag Manager, app subdomain |
| **Preload** | Critical hero image |
| **DNS Prefetch** | GitHub buttons CDN |
| **Web Manifest** | PWA-capable with icons, theme color `#7e3af2` |

### Structured Data (JSON-LD)

| Schema Type | Purpose |
|-------------|---------|
| `WebSite` + `SearchAction` | Site identity and search box |
| `SoftwareApplication` | Free tool metadata with pricing |
| `FAQPage` | 5 FAQ questions matching visible content exactly |
| `BreadcrumbList` | Home breadcrumb |

### Web App (`app.openplanningpoker.com`)

| Feature | Implementation |
|---------|---------------|
| **Dynamic Title** | Per-route titles: Home, Create/Join Game, Game Room, Voting Session, 404 |
| **Dynamic Meta Description** | Updates based on current route context |
| **Canonical URL** | `https://app.openplanningpoker.com/` |
| **Open Graph Tags** | Full set matching website branding |
| **Twitter Card** | `summary_large_image` |
| **robots.txt** | Allows all crawlers, references sitemap |
| **sitemap.xml** | Static routes + dynamic game URLs from database (up to 1000) |
| **Angular SSR (Server-Side Rendering)** | Prerendered HTML for `/` and `/game` routes for crawlers |
| **PWA** | Service worker with caching strategy, installable manifest |
| **Security Headers** | CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy, Cross-Origin-Resource-Policy |
| **Gzip Compression** | Enabled for text-based assets |
| **Cache Headers** | 1-year expiry with `immutable` for static assets, `no-cache` for HTML |
| **Semantic HTML** | `<header>`, `<main id="main-content">`, `<section>` with `aria-label`, skip-to-content link |
| **Breadcrumbs** | Dynamic breadcrumb navigation with `BreadcrumbList` structured data |

### Structured Data (Web App)

| Schema Type | Purpose |
|-------------|---------|
| `WebApplication` | App identity, free pricing, author |
| `BreadcrumbList` | Dynamic per-route breadcrumbs (Home → Game → Voting) |

## Dynamic Sitemap

The Game Engine serves `/sitemap.xml` with:
- Static routes (Home, Game creation page)
- Up to 1000 dynamic game URLs with `<lastmod>` dates from the database
- Appropriate `changefreq` and `priority` per URL type

## Performance Optimizations

- **CSS**: Tailwind CSS (purged/minified in production), Angular Material with lazy-loaded styles
- **JavaScript**: Angular bundles with code splitting, service worker for offline caching
- **Images**: Explicit dimensions, lazy loading, alt text
- **Fonts**: Google Fonts with `display=swap`, Material Icons from CDN
- **Caching**: Static assets 1-year immutable cache, HTML no-cache via nginx

## Analytics

### Google Analytics 4

| Property | GA4 ID | Domain |
|----------|--------|--------|
| Website | `G-03VCK0K926` | `openplanningpoker.com` |
| Web App | `G-DEXZ4JTESJ` | `app.openplanningpoker.com` |

### Microsoft Clarity

| Property | Clarity ID | Domain |
|----------|-----------|--------|
| Shared | `xmim6e317s` | Both domains |

Clarity provides heatmaps, session recordings, and user behavior insights.

## SEO Checklist (Google Lighthouse)

| Criterion | Status |
|-----------|--------|
| Valid `robots.txt` | ✅ Both domains |
| Valid `sitemap.xml` | ✅ Static + dynamic |
| Unique, descriptive title tags | ✅ Dynamic per route |
| Unique meta descriptions | ✅ Dynamic per route |
| `hreflang` tags | ✅ Website |
| Canonical tags | ✅ Both domains |
| Valid structured data | ✅ 5 schema types (website) + 2 (web app) |
| Mobile-friendly | ✅ Responsive Tailwind + Angular Material |
| HTTPS enforced | ✅ Via nginx 301 redirects |
| Semantic HTML landmarks | ✅ `<header>`, `<main>`, `<footer>`, `<section>`, `<nav>` |
| Accessible skip-to-content link | ✅ Web app |
| Proper 404 page | ✅ Both domains |
| Image alt text | ✅ All images |
| Page speed | ✅ SSR, PWA caching, gzip, lazy loading |
