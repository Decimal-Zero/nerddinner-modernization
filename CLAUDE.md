# CLAUDE.md

Context for Claude Code picking up this repo. Read this first, then read
`docs/02-plan/plan.md` and `docs/02-plan/decision-log.md` in full before
making any changes.

## What this is

A practice legacy .NET modernization engagement, run using Decimal Zero
LLC's assessment/plan/execute methodology against a real historical
codebase (NerdDinner, the classic ASP.NET MVC 4 sample app). It's both
reps for the engagement workflow and a public case study for
decimalzero.com. See the root `README.md` and `NOTICE.md` for full
background and license provenance (Microsoft Public License / Ms-PL).

**The `docs/` directory is the source of truth, not this file.** This
file is an index and a set of working conventions; it will go stale.
`docs/02-plan/plan.md` and `decision-log.md` are the living documents —
if anything here conflicts with them, they win.

## Current status: complete

**Both phases (M1–M11) are done and verified.** `NerdDinner` is now a
single ASP.NET Core / .NET 10 application at `src/NerdDinner`, tested by
`NerdDinner.Tests` at the repo root. The legacy .NET Framework codebase
this engagement started from no longer exists in the working tree —
recoverable via git history (the `bf314f5` baseline import and every
commit since), not gone forever.

- **Phase 1 (M1–M6, in-place upgrade to .NET Framework 4.8.x):** dead
  auth stack replaced, `Web.config` hardened, dependencies brought
  current, checked-in `.mdf`/`.ldf` files replaced with EF6 Migrations.
  Full before/after scoring in `docs/03-outcome/phase-1-exit-checkpoint.md`.
  Decision log: DL-001 through DL-019.
- **Phase 2 (M7–M11, strangler-fig cutover to ASP.NET Core / .NET 10):**
  a YARP reverse proxy (M7) let routes migrate one at a time — Home
  (M8), Dinners/RSVP/Search with EF Core + NetTopologySuite replacing
  EF6 + `DbGeography` (M9), Auth with ASP.NET Core Identity replacing
  ASP.NET Identity 2.x/OWIN (M10) — before the legacy app and the proxy
  were both decommissioned (M11) and the surviving project renamed from
  `NerdDinner.Proxy` to `NerdDinner`. Decision log: DL-020 through DL-031.
- **A parallel thread through M9/M10:** ~29 failures inside Visual
  Studio's own Test Explorer (passing fine via CLI `vstest.console.exe`)
  traced to one root cause — VS's IDE-hosted test AppDomain not
  resolving `AppDomain.CurrentDomain.BaseDirectory`/`ConfigurationManager`
  the way IIS or plain CLI does — showing up through four different
  mechanisms. Fixed via `Assembly.CodeBase`-based path resolution and
  explicit connection-string/config passing. Decision log: DL-023
  through DL-026. This class of problem is specific to the classic
  .NET Framework test project that no longer exists — not expected to
  recur, but worth knowing if something like it ever does.
- **Two documented, deliberately deferred gaps, neither blocking
  anything:** Bing Maps' free tier is retired with no in-plan
  replacement scoped (DL-030); a dinner with no geocoded location is
  legal by the model's own contract and the app tolerates it gracefully.

Read `docs/01-assessment/assessment.md` for the original scored
assessment this plan was built on. Read `decision-log.md` end to end for
the full decision history — it's long, and that's the point: it's the
actual evidence trail for the case study, not just a status summary.

## The two-phase approach, in one paragraph

Phase 1 upgraded in place to .NET Framework 4.8.x — lower risk, same
runtime, fixed the highest-value findings (dead auth stack, `Web.config`
misconfiguration, stale dependencies, zero test coverage). Phase 2 was a
genuine strangler-fig cutover to ASP.NET Core / .NET 10 via reverse-proxy
path routing, migrating route-by-route, auth last, then decommissioning
both the legacy app and the proxy once nothing depended on either. Full
reasoning for why this beat a single-step rewrite is in `decision-log.md`
DL-001 through DL-003.

## Working conventions for this repo

These conventions shaped how the engagement was actually run and remain
the standard for any further work on this codebase (bug fixes, the
deferred Azure Maps migration, etc.) — they don't stop applying just
because the milestone list is complete.

- **Every milestone in `plan.md` has acceptance criteria stated up
  front.** A milestone isn't done until those are met and verified —
  not just "code written." Check finished work against its acceptance
  criteria explicitly before considering it done.
- **Log significant decisions in `decision-log.md`**, not just in commit
  messages. Follow the existing entry format (Decision / Reasoning /
  Status, and Alternatives considered where relevant). This is what lets
  someone reconstruct *why* the codebase looks the way it does, not just
  *what* changed.
- **Characterization tests come first, changes come after — not the
  other way around** (DL-004). `NerdDinner.Tests` characterizes real
  observed behavior, bugs and all. Run the suite before and after any
  change. A newly-failing test is a signal to stop and explain what
  changed and why, not to push through or quietly update the test to
  match new behavior. If a change legitimately changes observable
  behavior, that's a decision-log entry, and the test gets updated
  deliberately with a comment explaining the change — never silently.
- **Document real bugs found; don't silently fix them mid-milestone.**
  Several pre-existing bugs (unhandled NREs in `DinnersController`/
  `RSVPController` on missing ids, `SearchController.JsonDinnerFromDinner`
  NREs on a dinner with no `Location`) were found during characterization
  and deliberately preserved, ported forward milestone to milestone, and
  characterized rather than fixed as a side effect — see DL-004 and
  DL-028.
- **No fabricated context.** The assessment's Category 10 (Business
  Criticality) is deliberately left unscored because there's no real
  business behind this codebase — an earlier draft invented one and it
  was reverted (see `decision-log.md` DL-005). Don't reintroduce
  invented business justification anywhere in `docs/`.
- **Verify live, not just by reasoning about the code.** Several real
  bugs in this engagement (the M8 YARP route-precedence bug/DL-022, the
  M9 nullable-`Point` EF Core requiredness bug/DL-028, the M10 Identity
  schema incompatibility and `RSVPs` validation bugs/DL-029) were only
  ever found by actually running the app and exercising the failing
  path — not by inspection, and not always by the first automated test
  written either. When in doubt, run it.

## Build and test

- Open `NerdDinner.sln` (Visual Studio 2022+/`dotnet` CLI, .NET 10 SDK).
  `src/NerdDinner` is the application; `NerdDinner.Tests` is the test
  project.
- `NerdDinner.Tests` requires LocalDB. Most tests create/drop dedicated
  databases per run (`NerdDinnerTests`, `NerdDinnerIdentityTests`); a
  handful of `Category=Integration` tests (`AuthFlowTests`) intentionally
  touch the shared dev `NerdDinner` database instead — see their class
  comments for why.
- Run the full suite:
  ```
  dotnet test NerdDinner.Tests/NerdDinner.Tests.csproj
  ```
- To run the app locally:
  ```
  dotnet run --project src/NerdDinner --urls http://localhost:5021
  ```
- Local secrets (GeoNames username, OAuth provider keys, Bing Maps key)
  are configured via `dotnet user-secrets` against `src/NerdDinner`'s own
  `UserSecretsId`, or directly in `appsettings.json` (all ship blank by
  default, same externalized-but-unconfigured pattern the legacy app
  used). None are required for the app to run or the default test suite
  to pass.

## What NOT to do without checking in

- Don't delete or rewrite existing `decision-log.md` entries to reflect
  new thinking — append a new entry that supersedes the old one and
  says so explicitly (see how DL-006, DL-007, and DL-021 were resolved
  as updates to their own entries or later entries, not deletions). The
  log's value is partly in showing how the thinking evolved, not just
  the final state.
- Don't treat a milestone's original acceptance criteria as immutable if
  reality genuinely changes underneath them (see DL-021's M8/M9 scope
  correction) — but any such change needs its own decision-log entry
  explaining why, not a silent edit.
