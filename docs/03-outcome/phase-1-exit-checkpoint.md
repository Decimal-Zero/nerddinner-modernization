# Phase 1 Exit Checkpoint — Re-Assessment

Per `docs/02-plan/plan.md`'s Phase 1 exit checkpoint: re-scores the
original assessment's Categories 1 (Platform and Framework Currency), 5
(Security), 7 (Dependencies and Integration Points), and 8 (Testing and
Quality Assurance) against the Phase-1-complete codebase (M1 through
M6, all adopted per `docs/02-plan/decision-log.md`). Same scoring
template, same 1–5 scale, same item set as
`docs/01-assessment/assessment.md`, so the before/after numbers are
directly comparable.

**Categories 2, 3, 4, 6, and 9 are not re-scored here** — Phase 1's
milestones didn't target them, and re-scoring categories nothing
touched would just restate the original numbers under a new date.
Category 10 remains deliberately unscored, per `decision-log.md`
DL-005 — no real business context exists for this practice engagement,
and inventing one now would be exactly the fabrication that entry
already rejected once.

Every score below cites the specific evidence checked directly in the
current repo (`packages.config`, `Web.config`, decision-log entries) —
same discipline the original assessment used, not a re-read of the plan
docs' own claims about themselves.

---

## Category 1: Platform and Framework Currency

| Item | Before | After | Notes / Evidence |
|---|---|---|---|
| Language/runtime version and support status | 1 | 3 | `TargetFrameworkVersion` is now `v4.8` (was `v4.5`, confirmed in `src/NerdDinner.csproj`). This is a genuinely different support posture, not just a version bump: .NET Framework 4.5 reached end of support in January 2016 — the original baseline was running on an *actively unsupported* runtime. 4.8 is the final Framework release and remains supported for the lifetime of the Windows versions it ships with. Still not under active feature development (unchanged), but "aging and supported" is a materially different finding than "aging and unsupported." |
| Web framework currency | 1 | 3 | `Microsoft.AspNet.Mvc` is now 5.3.0 (was 4.0.20710.0, ~2012) — confirmed in `packages.config`. MVC5 is the final release of classic ASP.NET MVC, in maintenance mode with patches tied to the Framework lifecycle, same reasoning as above. Several versions behind ASP.NET Core still — that gap is Phase 2's job, not something Phase 1 could or should have closed. |
| OS/server platform support | 2 | 2 | Unchanged. Still IIS/Windows-only, no containerization or cross-platform capability — architecturally unreachable from within Phase 1 by design (DL-001); this is exactly what Phase 2's ASP.NET Core cutover exists to fix. |
| Realistic in-place upgrade path | 1 | 2 | The specific fact this item measures — whether an incremental, same-runtime path exists all the way to ASP.NET Core — is technically unchanged: MVC5/.NET Framework 4.8 and ASP.NET Core still don't share a runtime, so the final step is still a port, not an upgrade. What *did* change: Phase 1 proved, executed, and verified (M3, 80/80 tests passing, zero build warnings) that the same-runtime half of the journey is a real, low-risk, already-completed path, and Phase 2's reverse-proxy strangler-fig approach (DL-003) is now a concretely scoped, de-risked plan rather than an open question. Scored 2, not higher, to avoid crediting planning maturity against a question that's specifically about whether a same-runtime path to full currency exists — it doesn't, and won't until Phase 2 lands. |
| Vendor/community support window remaining | 1 | 3 | Direct consequence of the first item: 4.8 has a real, ongoing support window (tied to Windows lifecycle); 4.5 did not. Still "maintenance mode, no new features" as a ceiling — that's inherent to .NET Framework itself, not something any Phase 1 milestone could change. |

**Category average: 1.2 → 2.6**

---

## Category 5: Security

| Item | Before | After | Notes / Evidence |
|---|---|---|---|
| Known vulnerabilities (CVE exposure) | 1 | 4 | jQuery is now 3.7.1 (was 1.7.1, 2011, multiple documented XSS CVEs) — the current version available through the NuGet package channel this repo restores from (DL-009 documents that 4.x exists on npm/CDN but not via NuGet; using what's actually installable here isn't a shortcut, it's the honest ceiling). DotNetOpenAuth — unmaintained since ~2014 — is completely removed (M4/DL-014), not just upgraded. |
| Authentication and authorization implementation | 2 | 4 | SimpleMembership (`WebSecurity`) and DotNetOpenAuth, both superseded since ASP.NET Identity shipped in 2014, are fully replaced with ASP.NET Identity 2.2.4 + OWIN 4.2.3 (M4/DL-014) — the standard, still-supported migration path for exactly this combination. Resource-level ownership checks (`IsHostedBy`) are unchanged and still consistently applied, confirmed by passing characterization tests. Not a 5: ASP.NET Identity 2.x is itself the *last* classic-ASP.NET-compatible identity system, one rung below ASP.NET Core Identity (Phase 2's eventual target), not the current state of the art in absolute terms. |
| Secrets and credential handling | 4 | 5 | Still no hardcoded credentials anywhere (unchanged discipline, now extended to more settings: `googleClientId`/`googleClientSecret` alongside the original three OAuth provider pairs). Genuinely improved, not just maintained: the GeoNames username is sourced from a real local secret store (`Microsoft.Configuration.ConfigurationBuilders.UserSecrets`, DL-013) rather than a blank placeholder a developer would otherwise be tempted to fill in directly in `Web.config` — a concrete mechanism for keeping a real secret out of source control entirely, not just an empty appSetting relying on developer discipline. |
| Input validation / injection protection | 4 | 4 | Unchanged — DataAnnotations validation, `[ValidateAntiForgeryToken]` on POST actions, and EF's parameterized-query-only data access are all still in place, confirmed by the characterization suite with no regressions found across M3–M6. |
| Encryption of data at rest and in transit | 2 | 4 | Every external call this app makes is now HTTPS: `GeolocationService`'s two API calls (M5/DL-016, including the non-obvious `secure.geonames.org` hostname requirement), the Bing Maps script tag (M5), and `NerdDinner.js`'s remaining client-side geocoding calls (DL-017). Not a 5: data-at-rest encryption for the local LocalDB databases was never addressed and isn't a Phase 1 target — there's no production database in this practice engagement for that to meaningfully apply to. |

**Category average: 2.6 → 4.2**

**Original critical flags — status of each:**
- `Web.config`: `<compilation debug="true">` — **Resolved for the path that matters.** The base `Web.config` still has `debug="true"` (appropriate for local development); `Web.Release.config`'s pre-existing `RemoveAttributes(debug)` transform (confirmed present and unmodified since the 2012 baseline, DL-016) forces `debug="false"` for Release builds, which is what would actually ship.
- `Web.config`: `<customErrors mode="Off">` — **Resolved.** Now `mode="RemoteOnly"` (DL-016).
- Hardcoded fallback IP literal (`"71.117.141.83"`) — **Resolved.** Removed outright, not replaced with another workaround (DL-016).

**A residual finding not eliminated, and not one of Phase 1's targets:** Glimpse (`Glimpse`, `Glimpse.AspNet`, `Glimpse.Mvc4`) is still installed and still active in `Web.config` — `defaultRuntimePolicy="On"`, its `HttpModule`/`HttpHandler` still registered, `glimpse.axd` still a live endpoint. M1's dependency research explicitly disposed of Glimpse as "remove entirely" (it's been unmaintained since ~2014), but that removal was never actually assigned to one of M1–M6's executed acceptance criteria — `plan.md`'s M5 scope named only `debug`/`customErrors`/`GeolocationService`/Bing Maps/the fallback IP, not Glimpse specifically. Its default `LocalPolicy` (restricting the diagnostic UI to local requests) is still active — not commented out — which meaningfully limits real-world exposure, but a live, abandoned diagnostics tool wired into the production request pipeline is exactly the kind of finding this category exists to catch, and it wasn't. Flagging this now rather than letting the higher category average paper over it.

---

## Category 7: Dependencies and Integration Points

| Item | Before | After | Notes / Evidence |
|---|---|---|---|
| Currency/support status of major libraries | 1 | 4 | Everything M1 identified as "upgrade in-place" is now at a current version: EntityFramework 6.5.2, MVC/Razor/WebPages 5.3.0/3.3.0-line, jQuery 3.7.1, jQuery UI 1.14.1, Knockout 3.5.3, Newtonsoft.Json 13.0.3 (all confirmed in `packages.config`). Not a 5: Glimpse (see above) remains genuinely stale and unaddressed — the one real exception to an otherwise thorough currency pass. |
| Number and health of external integrations | 2 | 3 | The two direct third-party API calls (geonames.org, ipinfodb.com) still have no abstraction layer and no retry/circuit-breaker — that architecture is unchanged. What did change: both calls are now wrapped in try/catch and return `null` on failure instead of letting an unhandled exception propagate to the caller (M5/DL-016) — the specific "no error handling" finding from the original assessment is resolved, even though the broader resilience gap isn't. |
| Unsupported/abandoned dependencies | 1 | 3 | DotNetOpenAuth — dead upstream since ~2014 — is completely removed (M4), not just upgraded around. Glimpse (3 packages, also dead since ~2014) remains installed and live (see Category 5 note above) — a second abandoned dependency that was identified in the same M1 research pass but never actually removed. One of two resolved is real progress, not a full fix. |
| Licensing status of components | 4 | 4 | Unchanged. All dependencies added since the original assessment (ASP.NET Identity/OWIN, the ConfigurationBuilders packages) are Microsoft-authored under the same permissive licensing terms as everything else in the tree. |
| Single points of failure among dependencies | 1 | 2 | `GeolocationService` still has no fallback provider, retry policy, or circuit breaker — the app still has a hard dependency on two free, un-SLA'd third-party services, unchanged architecturally. What changed: a failure there no longer takes the request down with it (M5's try/catch, same change noted above) — a real, if partial, mitigation of the "unmitigated" framing from the original finding, not a resolution of the underlying single-point-of-failure risk itself. |

**Category average: 1.8 → 3.2**

---

## Category 8: Testing and Quality Assurance

| Item | Before | After | Notes / Evidence |
|---|---|---|---|
| Automated test coverage (unit, integration) | 1 | 4 | `NerdDinner.Tests` (xUnit + Moq) didn't exist at all originally — `NerdDinner.sln` had a single project. It now has 80 tests passing, 0 skipped, covering model validation, all five controllers' observable behavior (including the ownership-check logic and the three real pre-existing bugs M2 found and deliberately left characterized, not silently fixed), the new ASP.NET Identity auth mechanics (registration, duplicate rejection, password validation, login), the `DbGeographyModelBinder` fix, and Integration-tagged tests against the live GeoNames/ipinfodb APIs. Not a 5: action-level flows that need a live OWIN context (`ExternalLogin` challenge/callback, `Manage`, `Disassociate`) remain untested at the controller level — a real, explicitly documented gap (`decision-log.md` DL-014), not a hidden one. |
| Presence/quality of staging or QA environment | 1 | 1 | Unchanged. No staging/QA environment exists or was ever in scope for Phase 1 — this is a Category 6 (Infrastructure and Deployment) concern, and none of M1–M6 touched deployment infrastructure. No basis to claim improvement here. |
| Manual test/regression process | 1 | 2 | Still no formal, written test plan or checklist artifact — that's unchanged. What does now exist, and didn't before: a documented command for running the regression suite while excluding live-API Integration tests (`vstest.console.exe .../TestCaseFilter:"Category!=Integration"`, written into `m2-characterization-tests.md`, `CLAUDE.md`, and the test files themselves), explicit guidance on when to run the Integration-tagged tests deliberately, and — in practice, over the course of this engagement — actual manual verification in the browser after each milestone (login/logout, protected pages, dinner creation, geocoding). Real progress from "nothing documented," but still short of an actual QA process. |
| Historical defect rate / incident frequency | N/A | N/A | Unchanged — still not assessable; no production history exists for this practice engagement. |

**Category average (excluding N/A): 1.0 → 2.33**

This is the largest swing in the whole re-assessment, and the original assessment called it the single most consequential finding for exactly this reason: establishing the test safety net first (DL-004) is what made every other Phase 1 milestone verifiable rather than a leap of faith. Concretely, it's also what caught the real, non-M3/M4-related `DbGeographyModelBinder` bug during manual testing after M4 (DL-015) and gave a fast, confident way to confirm the fix — the exact payoff this finding predicted.

---

## Updated Summary Scorecard (re-scored categories only)

| # | Category | Before | After | Recommendation (after) |
|---|---|---|---|---|
| 1 | Platform and Framework Currency | 1.2 | 2.6 | Update |
| 5 | Security | 2.6 | 4.2 | Leave alone (with one residual finding — see Glimpse note) |
| 7 | Dependencies and Integration Points | 1.8 | 3.2 | Update |
| 8 | Testing and Quality Assurance | 1.0 | 2.33 | Update |

Every re-scored category moved out of "replace/rebuild candidate" territory and into "update" or better — including Security, which crossed all the way into "leave alone" by the numeric guidance, with one explicitly-named exception (Glimpse) rather than a clean sweep. That asterisk is deliberate: the point of this checkpoint is to produce honest verification evidence for the case study, not a scorecard that reads better than the codebase actually is.

## What this checkpoint does not claim

- Categories 2 (Architecture), 3 (Code Quality), 4 (Data Layer), 6 (Infrastructure/Deployment), and 9 (Documentation) are **not re-scored** — nothing in M1–M6 targeted them, and Category 4's original spatial-data flag (`DbGeography` → NetTopologySuite) and Category 6's complete absence of deployment automation are both still exactly as they were, waiting on Phase 2.
- Category 10 (Business Criticality) remains unscored, per DL-005 — still no real business context to score against.
- The overall Phase 1 → Phase 2 rationale (DL-001 through DL-003) is unchanged by this checkpoint: it confirms the two-phase approach's Phase 1 half delivered what it promised, not that Phase 2 is now unnecessary.
