# Task: Expand the Skoleoverblikket marketing footer with legal and contact pages

## Context

The Skoleoverblikket marketing site is a React SPA (`web/src/`). Unauthenticated users see the public landing page (`LandingPage.tsx`); authenticated users see the app. Both live on the same domain/codebase — no subdomain split.

The current footer (inlined at the bottom of `LandingPage.tsx`) is minimal: logo, copyright, and a login link. It needs to be expanded into a proper footer with navigation links and four new pages.

Design system: Tailwind CSS with a custom `brand` green palette (see `tailwind.config.js`). Fonts: Lato (body), Playfair Display (headings). Match the visual style of the existing landing page exactly.

## What to build

### 1. Extract and expand the footer component

Extract the existing inline footer from `LandingPage.tsx` into `web/src/components/Footer.tsx`. Expand it to include a navigation section with these links:

- Om Skoleoverblikket → `/om`
- Privatlivspolitik → `/privatlivspolitik`
- Kontakt → `/kontakt`

Keep the existing logo, copyright line (`© {year} Skoleoverblikket · Data opbevares i EU`), and login link.

### 2. Register the new routes

In `web/src/App.tsx`, add three new public routes (no auth required, outside the Layout wrapper):

- `/om` → `OmPage`
- `/privatlivspolitik` → `PrivatlivspolitikPage`
- `/kontakt` → `KontaktPage`

### 3. Cookie consent banner

Add a minimal cookie consent banner component (`web/src/components/CookieBanner.tsx`) that:

- Renders a small, non-intrusive bar at the bottom of the screen on the public landing page only
- Informs the user that the site uses a login cookie (session only, no tracking)
- Has a single "Forstået" (OK) button
- On click: sets a `cookie_consent` key in `localStorage` and unmounts the banner
- On page load: if `cookie_consent` is already in `localStorage`, do not render the banner
- Uses only Tailwind classes, brand palette colors
- Never shown inside the authenticated app

Render `<CookieBanner />` inside `LandingPage.tsx`.

### 4. New pages

Create the following three page files in `web/src/pages/`. Each page must:
- Use the shared `<Footer />` component at the bottom
- Have a nav bar consistent with `LandingPage.tsx` (logo + login/signup buttons)
- Be fully responsive
- Use the brand palette and Lato/Playfair Display fonts

---

#### `/om` — OmPage.tsx

Content (write polished Danish copy from these bullet points):

- Skoleoverblikket er skabt af en frivillig IT-person fra en lille dansk friskole
- Professionel IT-udvikler med mange års erfaring
- Far til to børn — ét i friskole, ét i dagpleje — med et personligt forhold til skolernes hverdag
- Frustration over tunge, dyre og indviklede løsninger til noget så simpelt som et skema
- Målet: et redskab der er så enkelt, at skolesekretæren kan bruge det fra dag ét — uden oplæring

Tone: warm, personal, trustworthy. Not corporate. First-person singular or "vi" (your call — be consistent).

---

#### `/privatlivspolitik` — PrivatlivspolitikPage.tsx

Draft a GDPR-compliant Danish privacy policy for a B2B SaaS targeting Danish independent schools. Use the following facts:

**Data controller:** Skoleoverblikket (contact: kontakt@skoleoverblikket.dk)

**Personal data collected and processed:**
- Account data: name, email address (school administrators and staff)
- School data: class names, staff names, course names, room names, schedules
- Uploaded files (stored per school, only accessible to that school's members)
- Billing data: handled entirely by Stripe — Skoleoverblikket does not store payment card data
- Session cookie: used solely for authentication, no tracking

**Data processors (sub-processors):**
- Stripe (billing/payments) — subject to Stripe's own DPA
- OVHcloud (hosting and file storage, EU data centers)

**Access control:** all school data is strictly tenant-scoped. Only members of a given school can access that school's data.

**Data retention:** data is retained for the duration of the subscription. Upon cancellation, data is retained for 90 days to allow re-activation or export, then permanently deleted. (Note: automated deletion is not yet implemented — a manual process applies until then.)

**Data subject rights:** users may request access, correction, or deletion by emailing kontakt@skoleoverblikket.dk. Requests are handled within 30 days.

**Cookies:** one session cookie used for login. No analytics, no advertising cookies.

**Governing law:** Danish law. Supervisory authority: Datatilsynet (datatilsynet.dk).

Format as structured sections with headings (§1, §2, … or named sections — your call). Use plain, readable Danish. Avoid legalese where possible. This is read by school secretaries, not lawyers.

---

#### `/kontakt` — KontaktPage.tsx

A simple contact page. No form — just:

- A short warm intro (1–2 sentences): you're welcome to reach out with questions, feedback, or if you want to learn more.
- Email link: `kontakt@skoleoverblikket.dk` (styled as a prominent `mailto:` link, not buried in a paragraph)
- Response time expectation: "Vi svarer typisk inden for 1–2 hverdage"

Keep it brief and human. Do not add a contact form.

---

## Constraints

- No new dependencies
- No CSS files or inline `style` props — Tailwind only
- No mocking of `DbContext` or backend changes needed — this is frontend only
- Do not add analytics, tracking scripts, or any third-party embeds
- All new routes must be public (no auth required)
- Playwright `data-testid` attributes on: the cookie banner's dismiss button, and each footer navigation link

## Out of scope

- Backend changes
- Email infrastructure (the mailto link is sufficient)
- Automated data deletion (tracked separately as a follow-up task)
