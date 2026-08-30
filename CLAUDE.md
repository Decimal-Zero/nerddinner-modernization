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

## Current status

- **M1 (dependency compatibility research): complete.** See
  `docs/02-plan/m1-dependency-research.md` for the full audit and
  per-package disposition (upgrade in-place / replace / remove).
- **M2 (characterization test suite): complete and verified.**
  `NerdDinner.Tests` (xUnit + Moq) passes against LocalDB. See
  `docs/02-plan/m2-characterization-tests.md`.
- **M3 (framework and dependency upgrade to .NET Framework 4.8.x):
  complete and verified.** See `decision-log.md` DL-008 through DL-012
  for what that took and what was decided along the way.
- **M4 (auth stack): complete and verified.** SimpleMembership +
  DotNetOpenAuth replaced with ASP.NET Identity 2.2.4 + OWIN 4.2.3, per
  M1's determination. See `decision-log.md` DL-014. A pre-existing
  `DbGeographyModelBinder` crash (unrelated to M3/M4, dating to the 2012
  baseline) found by manual testing after this milestone was fixed
  separately — see DL-015.
- **M5 (security and configuration hardening): complete and verified.**
  `Web.config` hardening, HTTPS for `GeolocationService` and the Bing
  Maps script, fallback-IP removal, and graceful failure handling — see
  `decision-log.md` DL-016 (includes a real gotcha: GeoNames' HTTPS
  endpoint is a different hostname, `secure.geonames.org`, not just a
  protocol change on `api.geonames.org`).
- **M6 (data layer cleanup): complete and verified.** Checked-in
  `.mdf`/`.ldf` files removed; `NerdDinnerContext`'s schema and seed
  data now come from EF6 Migrations (`src/Migrations/`), applied via
  `MigrateDatabaseToLatestVersion` against a named LocalDB database
  instead of an attached file. See `decision-log.md` DL-018.
- **Phase 1 exit checkpoint: complete.** See
  `docs/03-outcome/phase-1-exit-checkpoint.md` — Categories 1/5/7/8 all
  moved out of "replace/rebuild candidate," with one named residual
  (Glimpse, still live, never actually removed despite M1 flagging it).
  **Phase 1 (M1–M6) is fully complete.**
- **M7 (reverse proxy scaffold): complete and verified.**
  `src-core/NerdDinner.Proxy` (ASP.NET Core / .NET 10, YARP) added
  alongside the legacy app; a catch-all route forwards every request to
  the legacy app, with only a diagnostic `/_proxy/health` endpoint
  handled locally. Verified live (both apps run, proxy forwards
  correctly), not just built — see `decision-log.md` DL-020.
- **M8 (migrate `Home`): complete and verified.** Narrowed from the
  original "`Home`, `Search`" scope — `Search` moved to M9, it's a
  data-layer migration, not a routing one (DL-021). `HomeController` +
  views ported into `NerdDinner.Proxy`, YARP catch-all given explicit
  low priority (`"Order": 1000`) after a real bug where it was silently
  swallowing every request including literal paths (DL-022). New
  `NerdDinner.Proxy.Tests` project (`Category=Integration`, needs the
  legacy app running on `localhost:10581`) verifies real HTTP routing
  through the proxy — 4/4 passing. M9 (migrate Dinners, RSVP, and
  Search) is next — see `plan.md`.
- Full suite: 80 passed, 0 skipped (`Category!=Integration` filter —
  the GeoNames/ipinfodb Integration tests need your own locally-stored
  GeoNames username, per DL-013, and aren't part of the default fast
  run).

Read `docs/01-assessment/assessment.md` for the full scored assessment
this plan is built on, including the six-category "replace/rebuild
candidate" findings that motivate the two-phase approach.

## The two-phase approach, in one paragraph

Phase 1 upgrades in place to .NET Framework 4.8.x — lower risk, same
runtime, fixes the highest-value findings (dead auth stack, `Web.config`
misconfiguration, stale dependencies, zero test coverage). Phase 2 is a
genuine strangler-fig cutover to ASP.NET Core / .NET 10 via reverse-proxy
path routing, migrating route-by-route, auth last. Full reasoning for
why this beats a single-step rewrite is in `decision-log.md` DL-001
through DL-003.

## Working conventions for this repo

- **Every milestone in `plan.md` has acceptance criteria stated up
  front.** A milestone isn't done until those are met and verified —
  not just "code written." If you finish a milestone's work, check it
  against its acceptance criteria explicitly before considering it done.
- **Log significant decisions in `decision-log.md`**, not just in commit
  messages. Follow the existing entry format (Decision / Reasoning /
  Status, and Alternatives considered where relevant). This is what lets
  someone reconstruct *why* the codebase looks the way it does, not just
  *what* changed.
- **Characterization tests come first, changes come after — not the
  other way around** (DL-004). `NerdDinner.Tests` characterizes the
  *original* baseline behavior, bugs and all. Run the suite before and
  after any change. A newly-failing test is a signal to stop and explain
  what changed and why, not to push through or quietly update the test
  to match new behavior. If a dependency upgrade legitimately changes
  observable behavior, that's a decision-log entry, and the test gets
  updated deliberately with a comment explaining the change — never
  silently.
- **Document real bugs found; don't silently fix them mid-milestone.**
  Three were found writing M2 (see `m2-characterization-tests.md`):
  unhandled NREs in `DinnersController.DeleteConfirmed` and
  `RSVPController.Register` on missing ids, and
  `SearchController.JsonDinnerFromDinner` NPEs on a dinner with no
  `Location`. These get fixed as part of whatever milestone naturally
  covers them, with the test's `Assert.Throws` updated deliberately at
  that point — not patched opportunistically now.
- **No fabricated context.** The assessment's Category 10 (Business
  Criticality) is deliberately left unscored because there's no real
  business behind this codebase — an earlier draft invented one and it
  was reverted (see `decision-log.md` DL-005). Don't reintroduce
  invented business justification anywhere in `docs/`.
- **No double-dashes inside XML comments** (`.csproj`, `.config`, etc.)
  — XML disallows `--` inside `<!-- -->` blocks and it silently breaks
  project loading in Visual Studio. Bit us once already in
  `NerdDinner.Tests.csproj`.
- **Known test gaps are deliberate, not oversights** — read the "Known
  issue" and "Documented gaps" sections of `m2-characterization-tests.md`
  before assuming something is undertested by accident (e.g.
  `AccountController`'s SimpleMembership-dependent flows, deliberately
  deferred to M4). `GeolocationService`'s two GeoNames tests were
  previously skipped pending the `ws.geonames.org` → `api.geonames.org`
  fix — that swap is done and confirmed drop-in, GeoNames' added
  username requirement is wired to the local user-secrets store (see
  `decision-log.md` DL-013), and both tests now run and pass against the
  live API (still `Category=Integration`, so still excluded from the
  default fast run).

## Build and test

- Open `NerdDinner.sln` in Visual Studio. Restore NuGet packages before
  first build.
- `NerdDinner.Tests` requires LocalDB (ships with Visual Studio). It
  creates, seeds, and drops a dedicated `NerdDinnerTests` database per
  run — never touches the `.mdf`/`.ldf` files under `src/App_Data`
  (those are legacy artifacts scheduled for removal in M6, not a test
  fixture).
- To exclude the network-dependent integration tests from a normal run:
  ```
  vstest.console.exe NerdDinner.Tests\bin\Debug\NerdDinner.Tests.dll /TestCaseFilter:"Category!=Integration"
  ```

## What NOT to do without checking in

- Don't jump ahead to Phase 2 work while Phase 1 milestones remain.
- Don't change the target of Phase 1 away from .NET Framework 4.8.x, or
  the Phase 2 target away from ASP.NET Core / .NET 10, without a
  decision-log entry explaining why — these were deliberate, discussed
  choices (see DL-001, DL-002), not defaults.
- Don't delete or rewrite existing `decision-log.md` entries to reflect
  new thinking — append a new entry that supersedes the old one and
  says so explicitly (see how DL-006 and DL-007 were resolved as
  updates to their own entries, not deletions). The log's value is
  partly in showing how the thinking evolved, not just the final state.
