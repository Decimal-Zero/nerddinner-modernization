# Modernization Plan — NerdDinner

Built directly on `docs/01-assessment/assessment.md`. See that document for
the full scoring and rationale; this plan translates its Suggested
Modernization Approach into sequenced, individually verifiable milestones.

Rationale for significant choices lives in `decision-log.md`, not
repeated here — this document is the "what and in what order," the
decision log is the "why."

## Scope

Two phases, corresponding to the assessment's recommended approach:

- **Phase 1 — In-place modernization on .NET Framework 4.8.x.** Fixes the
  highest-value findings from the assessment (auth stack, `Web.config`
  misconfiguration, dependency currency, zero test coverage) while
  staying on the same runtime — lower risk than a cross-runtime port.
- **Phase 2 — Strangler-fig cutover to ASP.NET Core / .NET 10.** Only
  begins once Phase 1 has removed the framework-currency and
  test-coverage blockers that made a single-step cutover risky.

Each milestone below states its acceptance criteria up front. A
milestone isn't complete until its criteria are met and verified — not
merely "code written."

---

## Phase 1 — In-place modernization (.NET Framework 4.8.x)

### M1 — Dependency compatibility research

The open research item carried over from the assessment: determine
whether the codebase's outdated dependencies have supported in-place
upgrade paths on .NET Framework 4.8, or require replacement even at this
stage.

**Investigate:**
- DotNetOpenAuth (abandoned upstream) — is there a maintained fork, or
  does it need replacing now rather than in Phase 2?
- EntityFramework 5.0.0 → latest EF6-line version compatible with
  `System.Web` / `System.Data.Entity` (not EF Core — that's a Phase 2
  concern).
- Front-end libraries (jQuery, jQuery UI, Knockout, Modernizr,
  jQuery.Validation) → current supported versions and any breaking
  changes that affect the existing views.
- ASP.NET MVC 4 → latest MVC5-line version, and what that upgrade
  actually requires (config changes, `web.config` handler updates, etc.)
- Whether ASP.NET Identity (the SimpleMembership successor) is a
  realistic in-place replacement on .NET Framework 4.8, or whether it's
  more efficient to defer identity replacement to Phase 2's ASP.NET Core
  Identity and keep SimpleMembership through Phase 1 as a deliberate,
  documented exception.

**Acceptance criteria:**
- A findings note (`m1-dependency-research.md`, in this directory) lists
  each flagged dependency, its target version, and one of: *upgrade
  in-place*, *replace now*, or *defer replacement to Phase 2* — with
  reasoning for each.
- Any "defer to Phase 2" decision is logged in `decision-log.md`.

### M2 — Characterization test suite

Establish the verification safety net **before** any behavioral change,
against the application's current (pre-upgrade) behavior — not the
behavior we intend it to have.

**Acceptance criteria:**
- A test project exists in the solution.
- Characterization tests cover the observable behavior of all five
  controllers (`Home`, `Dinners`, `RSVP`, `Search`, `Account`),
  including the ownership-check logic (`IsHostedBy`) and validation
  rules on `Dinner`/`RSVP`.
- Tests pass against the current, unmodified baseline (commit `bf314f5`
  or later, pre-Phase-1-changes).
- Known-fragile areas flagged in the assessment (`GeolocationService`'s
  unhandled external-API failure paths) have tests that document
  *current* behavior on failure, even if that behavior is bad — the
  point is a truthful baseline, not a pre-emptive fix.

### M3 — Framework and dependency upgrade

Execute the upgrade path M1 determined.

**Acceptance criteria:**
- `TargetFrameworkVersion` is 4.8.x.
- All "upgrade in-place" dependencies from M1 are at their target
  versions.
- Mobile-specific views (`Index.Mobile.cshtml`, `_Layout.Mobile.cshtml`)
  and the `jquery.mobile`/`jquerymobile` bundle registrations are
  removed, per DL-007. Desktop views/layout require no structural
  changes beyond removing Modernizr/yepnope references.
- M2's characterization test suite passes unchanged (or with only
  deliberate, individually-justified updates where an upgraded
  dependency legitimately changes observable behavior — each such change
  logged in `decision-log.md`).

### M4 — Auth stack

Per M1's determination: either replace SimpleMembership/DotNetOpenAuth
with ASP.NET Identity now, or confirm the deferral decision and skip to
M5.

**Acceptance criteria (if replacing now):**
- Login, registration, and external OAuth login flows pass their
  characterization tests (updated only where the auth mechanism change
  legitimately alters observable behavior, e.g. session cookie format).
  **Done for login/registration mechanics** — `AccountControllerIdentityTests`
  exercises `ApplicationUserManager` directly against LocalDB (see
  decision-log.md DL-014). External OAuth login flows remain
  uncharacterized at the controller level: they need a live OWIN
  context (`HttpContext.GetOwinContext()`), same class of limitation
  `AccountControllerTests` already had pre-M4 for a different reason —
  a real, documented gap carried forward, not a new one.
- No hardcoded secrets introduced; OAuth provider keys remain externalized
  to config, consistent with the assessment's finding that this was
  already handled correctly. **Done** — `googleClientId`/`googleClientSecret`
  added alongside the existing three provider key pairs, all blank by
  default (DL-014).

### M5 — Security and configuration hardening

Fixes the critical flags called out in the assessment.

**Status: complete and verified.** See `decision-log.md` DL-016.

**Acceptance criteria:**
- `Web.config`: `compilation debug="false"` for release configuration.
  **Done** — already handled by the pre-existing (unmodified since the
  2012 baseline) `Web.Release.config` transform; confirmed correct.
- `Web.config`: `customErrors` set to `On` or `RemoteOnly`, not `Off`.
  **Done** — set to `RemoteOnly`.
- `GeolocationService` calls use HTTPS, not plain HTTP. **Done** — see
  DL-016 for the `secure.geonames.org` hostname gotcha found doing this.
- `GeolocationService.PlaceOrZipToLatLong`'s hardcoded `ws.geonames.org`
  endpoint is updated to `api.geonames.org` — the `ws` subdomain has been
  retired (found while verifying M2's test suite; see
  `m2-characterization-tests.md`). **Already done, ahead of this
  milestone** — confirmed drop-in (response format unchanged), and the
  username `api.geonames.org` now requires is sourced from a local
  user-secrets store rather than checked in (decision-log.md DL-013).
- The Bing Maps script reference in `_Layout.cshtml` (`http://ecn.dev.virtualearth.net/...`)
  is updated to HTTPS — same insecure-transport pattern as
  `GeolocationService`, found during the M1/DL-007 view review. **Done.**
- The hardcoded fallback IP literal is removed. **Done.**
- `GeolocationService` has basic error handling around external API
  failures (no longer relies on `.First()` throwing an unhandled
  exception) — behavior change here is expected and should be reflected
  in updated characterization tests, not treated as a regression.
  **Done** — see DL-016 for the updated test.

### M6 — Data layer cleanup

**Status: complete and verified.** See `decision-log.md` DL-018.

**Acceptance criteria:**
- The checked-in `.mdf`/`.ldf` binary database files are removed from
  the repository (from the working tree going forward — no history
  rewrite of the baseline import commit). **Done.**
- A migration/seed-script mechanism (EF6 Migrations) reproduces the
  schema and any needed sample data. **Done** — `Migrations/Configuration.cs`
  + a scaffolded `InitialCreate` migration, verified end to end against
  a disposable LocalDB database (schema, `DbGeography` column, and seed
  data all confirmed working).
- `.gitignore` updated to prevent local database files from being
  re-committed. **Done** — `*.mdf`/`*.ldf`/`*.ndf`.

### Phase 1 exit checkpoint

**Status: complete.** See `docs/03-outcome/phase-1-exit-checkpoint.md`
for the full before/after scoring. Summary: Category 1 (Platform
Currency) 1.2 → 2.6, Category 5 (Security) 2.6 → 4.2, Category 7
(Dependencies) 1.8 → 3.2, Category 8 (Testing) 1.0 → 2.33 — every
re-scored category moved out of "replace/rebuild candidate," with one
explicitly-named residual finding (Glimpse, still installed and live,
never assigned to any of M1–M6's actual acceptance criteria despite
M1 disposing of it as "remove entirely").

Before Phase 2 begins: re-run the assessment's Categories 1, 5, 7, and 8
(Platform Currency, Security, Dependencies, Testing) against the
Phase-1-complete codebase. Document the before/after scores in
`docs/03-outcome/` — this is the first real verification evidence for
the case study, not just a status update.

---

## Phase 2 — Strangler-fig cutover to ASP.NET Core / .NET 10

Sequencing deliberately starts with the lowest-risk routes and ends with
the highest-risk (auth), so that the pattern is proven before the
hardest part is attempted.

### M7 — Reverse proxy scaffold

**Status: complete and verified.** See `decision-log.md` DL-020.

**Acceptance criteria:**
- A new ASP.NET Core (.NET 10) project exists alongside the Phase-1
  application. **Done** — `src-core/NerdDinner.Proxy`.
- A reverse proxy (YARP or equivalent) routes requests by path between
  the legacy app and the new app, with the new app currently handling no
  routes (proves the routing infrastructure works before migrating
  anything). **Done** — YARP catch-all route forwards everything to the
  legacy app; the new app's only local endpoint is a diagnostic
  `/_proxy/health` check, not a migrated business route.
- Both apps run and are reachable through the single proxy entry point
  locally. **Done** — verified live: legacy app under IIS Express
  (`localhost:10581`) and the new app (`localhost:5021`) both running,
  with `localhost:5021/` and `localhost:5021/Dinners` proxied through to
  the legacy app (200, correct page title) and `localhost:5021/_proxy/health`
  answered directly by the new app.

### M8 — Migrate stateless/read-only routes

**Status: complete and verified.** See `decision-log.md` DL-022.

Target: `Home` only. **Narrowed from the original "`Home`, `Search`"
scope — see `decision-log.md` DL-021.** `Search` is genuinely a
data-layer migration (it queries `Dinner`/`RSVP` via a `DbGeography`
spatial query, the exact same concern M9 already owns), not a routing
concern, so it moves to M9 alongside `Dinners`/`RSVP` rather than being
split across two milestones by data-access technology mid-migration.

**Acceptance criteria:**
- `Home` routes are served by the ASP.NET Core app via the proxy; all
  other routes (including `Search`) remain on the legacy app. **Done** —
  `HomeController`/`Views/Home` ported into `NerdDinner.Proxy`, routed
  via an explicit `controller=Home`-constrained conventional route,
  ordered ahead of the YARP catch-all.
- The `_Layout` rewrite required by the ASP.NET Core port (bundling
  mechanism differs entirely from `System.Web.Optimization`) includes
  responsive design, per DL-007 — this is where that deferred work
  lands, since the layout is being rebuilt regardless. **Done** — the
  existing `Site.css` (carried over unchanged) already had a complete
  `@media (max-width: 850px)` responsive block from the earlier mobile
  effort; the rebuilt `_Layout.cshtml` keeps the viewport meta tag and
  the same markup structure that block targets.
- Characterization tests for `Home` pass against the new implementation
  (via the proxy, exercising the real routing path — not the new app in
  isolation). **Done** — `NerdDinner.Proxy.Tests` (new project) hosts the
  real proxy app via `WebApplicationFactory` and asserts against actual
  HTTP responses. Originally confirmed via `/Dinners` (unmigrated at the
  time) reaching the legacy app through the same running instance; that
  assertion moved to `/Account/Login` once M9 migrated `Dinners` — see
  DL-028.

### M9 — Migrate Dinners, RSVP, and Search (CRUD + spatial data)

**Status: complete and verified.** See `decision-log.md` DL-028.

`Search` folded in from M8's original scope — see DL-021. It shares
this milestone's exact data-layer concern (spatial queries against
`Dinner`/`RSVP` via EF Core once that lands), so it's genuinely a better
fit here than split off with the stateless routes.

**Acceptance criteria:**
- `Dinners`, `RSVP`, and `Search` routes are served by the ASP.NET Core
  app. **Done** — all three controllers ported into `NerdDinner.Proxy`;
  `Search`'s two GET actions (by lat/long, by place name/zip) folded into
  one dispatching action to work around an ASP.NET Core routing
  limitation classic Web API didn't have (DL-028) — externally, the URL
  contract NerdDinner.js depends on is unchanged.
- EF Core data access replaces EF6; `Dinner.Location` uses a
  NetTopologySuite-based spatial type in place of `DbGeography`, with
  behavior verified against characterization tests covering location
  storage and retrieval specifically (this was flagged in the assessment
  as the easiest migration detail to miss). `Search`'s spatial query
  (`FindByLocation`) is the first real exercise of this new data layer,
  not `Dinners`/`RSVP` — worth verifying in that order given the
  dependency. **Done** — verified live against the actual shared dev
  database (not just reasoned about): EF Core + NetTopologySuite
  correctly read the existing `geography` column data the legacy EF6
  Migrations created, and a spatial `.Distance()` query correctly
  translates to SQL Server's `STDistance` and filters by real distance.
  New tests exercise this directly (`SearchByLocation_FindsNearbyDinner_WithinDistanceThreshold`,
  `..._ExcludesDinner_OutsideDistanceThreshold`,
  `..._ReturnsCorrectCoordinates_ForMatchedDinner`) — coverage the legacy
  suite could never have, since its `GeolocationService` had no
  dependency-injection seam to test around.
- Ownership-check logic (`IsHostedBy`) is preserved and tested. **Done**
  — same logic, ported verbatim; characterized via both direct
  controller tests (`EditGet_ReturnsInvalidOwnerView_WhenCurrentUserIsNotHost`,
  etc.) and a real-HTTP test confirming the actual Edit view renders for
  the correct owner. That real-HTTP test was originally
  `ViewRenderingTests.DinnersEdit_RendersWithoutError_ForHostUser` (a
  fake auth scheme, M9); M10 retired it in favor of
  `AuthFlowTests.EditView_RendersForRealOwner_AfterRealCreateAndOwnershipCheck`,
  which exercises the identical concern through real registration/login
  instead — see DL-029.

### M10 — Migrate Auth (highest risk, migrated last)

**Status: complete and verified.** See `decision-log.md` DL-029.

**Acceptance criteria:**
- `Account` routes and all authentication (including any OAuth providers
  carried forward) are served by the ASP.NET Core app using ASP.NET Core
  Identity. **Done** — `AccountController` + all Account views ported
  into `NerdDinner.Proxy`; local username/password login, registration,
  and logoff are live-tested. The four external OAuth providers
  (Google, Microsoft, Twitter, Facebook) are structurally ported with
  the same conditional-on-configured-secret wiring as the legacy app,
  but stay untested at the live level — no provider secrets are
  configured in this dev environment, same limitation the legacy app's
  own test suite already had.
- Login, registration, and external login characterization tests pass.
  **Done for login/registration/logoff** —
  `AccountControllerIdentityTests` (ported from the legacy suite) and
  new `AuthFlowTests` (real end-to-end HTTP, see below). External login
  flows remain uncharacterized at the live level, same documented gap
  carried forward from the legacy suite (no OAuth secrets configured).
- Session handling across the proxy boundary during the transition
  period (if any) is explicitly tested, not assumed. **Done, and the
  answer turned out to be "no transition-period concern remains"** —
  with `Account` migrated, every legacy controller (`Home`, `Dinners`,
  `RSVP`, `Search`, `Account`) is now served by the new app, so there's
  no cross-app session boundary left for a logged-in user to cross.
  What M8/M9 documented as an interim gap (DL-022/DL-028: `[Authorize]`
  never recognizing a real user) is resolved for real, verified live:
  `AuthFlowTests.RegisteredUser_SessionIsRecognized_ByADifferentAlreadyMigratedController`
  registers a user via `AccountController` and confirms the SAME session
  is recognized by `DinnersController.Create` — a genuinely different
  controller — through real ASP.NET Core Identity cookie auth, not a
  fake scheme.

### M11 — Decommission legacy app

**Status: complete and verified.** See `decision-log.md` DL-031.

**Acceptance criteria:**
- All routes are served by the ASP.NET Core app. **Done** — confirmed
  live standalone (no legacy app, no proxy, nothing else running):
  Home, Dinners, RSVP, Account, and `api/Search` all serve correctly.
- The reverse proxy and legacy Framework app are removed from the
  running system. **Done** — `src/` (the legacy Framework app) and the
  YARP reverse-proxy plumbing (`Program.cs`, `appsettings.json`, the
  `Yarp.ReverseProxy` package reference) are all removed. The renamed
  `NerdDinner` app (formerly `NerdDinner.Proxy`, see DL-031) is now the
  sole application.
- Glimpse (`Glimpse`, `Glimpse.AspNet`, `Glimpse.Mvc4`) is gone along
  with it — unmaintained since ~2014, disposed of as "remove entirely"
  back in M1, but never assigned to any Phase 1 milestone's actual
  acceptance criteria and so never actually removed. Flagged as a named
  residual finding in the Phase 1 exit checkpoint
  (`docs/03-outcome/phase-1-exit-checkpoint.md`); called out explicitly
  here rather than left to happen implicitly when the legacy Framework
  app is deleted, so it can't quietly get missed a second time. **Done**
  — Glimpse only ever existed in the deleted legacy app; confirmed gone
  by deleting `src/` and separately confirming `glimpse.axd` now 404s
  with nothing to forward it to.
- Full characterization suite passes against the final, sole application.
  **Done** — 39/39 in the renamed `NerdDinner.Tests`, run directly
  against `NerdDinner` with no proxy/legacy dependency anywhere in the
  loop.

**Not in scope for M11, deliberately:** replacing Bing Maps (its free
tier is retired) with a working mapping provider such as Azure Maps —
a real gap in the modernized app, but not one any milestone in this
plan currently owns. Documented as a deferred finding, with a note on
why the original assessment/M1 never caught it, in `decision-log.md`
DL-030. The app already degrades gracefully without it (a `Dinner` with
no geocoded `Location` is legal), so it doesn't block decommissioning
the legacy app — it's a follow-up modernization item, not a blocker.

---

## Milestone summary

| # | Milestone | Phase |
|---|---|---|
| M1 | Dependency compatibility research | 1 |
| M2 | Characterization test suite | 1 |
| M3 | Framework and dependency upgrade | 1 |
| M4 | Auth stack | 1 |
| M5 | Security and configuration hardening | 1 |
| M6 | Data layer cleanup | 1 |
| — | Phase 1 exit checkpoint | 1 |
| M7 | Reverse proxy scaffold | 2 |
| M8 | Migrate stateless/read-only routes (`Home`) | 2 |
| M9 | Migrate Dinners, RSVP, and Search (CRUD + spatial data) | 2 |
| M10 | Migrate Auth | 2 |
| M11 | Decommission legacy app | 2 |
