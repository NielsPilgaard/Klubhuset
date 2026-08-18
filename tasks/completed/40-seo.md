---
title: 'SEO: og tags, sitemap, robots.txt, per-route meta'
purpose: 'Make public marketing pages properly indexable and shareable; keep gated/tokenized routes out of search results.'
description: >-
  index.html has partial static OG/meta tags, og-image.png now exists, but
  every route still shares the same title/description (no per-route meta).
  Add per-route meta via react-helmet-async, add robots.txt + sitemap.xml,
  noindex gated/tokenized routes, and add SoftwareApplication JSON-LD to
  the landing page.
status: 'Proposed'
---

# SEO: og tags, sitemap, robots.txt, per-route meta

## TL;DR

1. Install `react-helmet-async`, wrap `App`, give `/`, `/om`, `/kontakt`,
   `/privatlivspolitik` distinct title/description/og/canonical.
2. `noindex,nofollow` on `/udskriv/*`, `/invitation/:token`,
   `/board-invitation/:token`, `/parent-invitation/:token`, `/login`,
   `/signup`, `/setup` — tokenized or thin pages, shouldn't rank.
3. Generate missing `web/public/og-image.png` (1200×630, brand colors +
   logo + tagline) via Playwright screenshot of a throwaway HTML page.
4. Add `web/public/robots.txt` (allow all + sitemap ref) and
   `web/public/sitemap.xml` (static, 4 public URLs).
5. Add `SoftwareApplication` JSON-LD to `LandingPage.tsx` with real
   pricing (499 kr/md incl. moms).
6. Keep existing static `index.html` meta as the pre-hydration fallback.

## Context — findings from explore + grill

- `web/index.html` already has `description`, `og:title`, `og:description`,
  `og:type`, `og:image` (points at `/og-image.png`, now built), `twitter:card`.
  No `og:url`, `og:site_name`, canonical link. Same block applies to every
  route — SPA, client-side routing only, no SSR/prerendering.
- React 18.3 (no native `<title>`/`<meta>` hoisting — that's React 19).
  No head-management lib installed. Chose `react-helmet-async` over a
  hand-rolled hook for correctness on unmount/restore ordering.
- Public routes (`App.tsx`): `/`, `login`, `signup`,
  `invitation/:token`, `parent-invitation/:token`,
  `board-invitation/:token`, `om`, `privatlivspolitik`, `kontakt`,
  `udskriv/klasse/:classId`, `udskriv/medarbejder/:staffId`,
  `udskriv/lokale/:roomId`, `udskriv/sfo`, `udskriv/ugeplan`, `setup`.
  Everything else requires auth (redirects to `/login` without a
  session) — robots.txt doesn't need to Disallow those, crawlers can't
  reach them anyway.
- `/udskriv/*` (print views) and `/invitation/:token` variants leak
  per-tenant data via URL — must not be indexed even though technically
  reachable without login.
- Prod domain: `https://skoleoverblikket.dk` (confirmed via
  `appsettings.json` `BaseUrl` and Keycloak authority).
- Brand palette exists in `web/tailwind.config.*` (`brand.50`…`brand.900`
  green scale) — reuse for og-image graphic instead of picking new colors.
- Pricing (`LandingPage.tsx`): `499 kr/md for Basis. Ingen
  bindingsperiode. Ingen skjulte gebyrer.` — source for JSON-LD `offers`.

## Decisions from grilling

- og-image: designed graphic (logo + tagline + brand bg), not an app
  screenshot — static, won't go stale when UI changes. I generate it via
  Playwright screenshot of a local HTML page, not hand-designed.
- Per-route dynamic meta, not just fixing the static block — `/om` and
  `/kontakt` deserve their own title/description for real SEO value.
- `react-helmet-async` over hand-rolled hook, despite extra dependency —
  more correct edge-case handling.
- noindex list: print pages + all three invitation token routes +
  login/signup/setup. Login/signup get a plain generic title but stay
  noindexed (thin content, not worth ranking).
- robots.txt: allow-all + sitemap reference only. No explicit Disallow
  list for gated routes — auth already blocks crawlers, redundant rule
  adds no protection.
- sitemap.xml: hand-written static file, not generated at build time —
  only 4 URLs, changes rarely.
- JSON-LD: include now (folded into this task after clarifying what it
  is) rather than deferred — same area of code, small addition.

## Proposed scope

### Frontend

- `npm install react-helmet-async`; wrap `<HelmetProvider>` around
  `<App>` (or inside it, above `BrowserRouter`).
- New small `SeoMeta` component (or inline `<Helmet>` per page) taking
  `title`, `description`, `path` (for canonical + og:url), optional
  `noindex`. Renders `<title>`, `<meta name="description">`,
  `<meta property="og:title">`, `og:description`, `og:url`,
  `og:site_name`, `<link rel="canonical">`, and
  `<meta name="robots" content="noindex,nofollow">` when `noindex` is
  true.
- Add `<SeoMeta>` to `LandingPage.tsx`, `OmPage.tsx`, `KontaktPage.tsx`,
  `PrivatlivspolitikPage.tsx` with distinct copy per page.
- Add `noindex` `<SeoMeta>` (or bare `<Helmet>` robots tag) to
  `PrintSchemaPage.tsx`, `SfoPrintPage.tsx`, `UgeplanPrintPage.tsx`,
  `InvitationAcceptPage.tsx` (covers both invitation routes),
  `BoardInvitationPage.tsx`, `LoginPage.tsx`, `SignupPage.tsx`,
  `SchoolSetupWizardPage.tsx`.
- Add `SoftwareApplication` JSON-LD (`<script type="application/ld+json">`
  via Helmet) to `LandingPage.tsx`: name, applicationCategory
  `BusinessApplication`, offers (499 DKK/month), description.
- `web/index.html`: add `og:url` (`https://skoleoverblikket.dk/`),
  `og:site_name` (`Skoleoverblikket`), `<link rel="canonical"
  href="https://skoleoverblikket.dk/">` as the pre-hydration fallback
  (helmet overrides per-route after mount).

### Static assets (`web/public/`)

- `og-image.png` — 1200×630, brand green background, real logo (house/grid
  icon + serif wordmark), headline "Billig og enkel skoleadministration"
  with subline "Skema, kalender og forældrekontakt". Built via a throwaway
  local HTML file + Playwright screenshot. **Done** — see
  `web/public/og-image.png`.
- `robots.txt`:
  ```
  User-agent: *
  Allow: /

  Sitemap: https://skoleoverblikket.dk/sitemap.xml
  ```
- `sitemap.xml` — 4 `<url>` entries: `/`, `/om`, `/kontakt`,
  `/privatlivspolitik`, each with `<loc>`, `<changefreq>`, `<priority>`.

## Verification

Per AGENTS.md: `/verify` (TypeScript build, dotnet format, dotnet build,
API integration tests) then `/test` (Playwright e2e) before declaring
done. No backend changes in this task, so `/verify`'s dotnet steps are a
no-op check, not a no-op skip. Manually confirm: view-source (or React
DevTools) shows correct per-route `<title>`, og-image renders in a social
share debugger, `/udskriv/sfo` and `/login` carry `noindex`, robots.txt
and sitemap.xml are served from the built app.
