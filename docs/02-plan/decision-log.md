# Decision Log — NerdDinner Modernization

Append-only. Each entry captures a significant decision, the reasoning
behind it, and what alternatives were considered. `plan.md` states what
the plan is; this document states why it's shaped that way.

---

### DL-001 — Two-phase modernization instead of a single-step rewrite

**Decision:** Modernize in two phases — (1) in-place upgrade to .NET
Framework 4.8.x, then (2) strangler-fig cutover to ASP.NET Core/.NET 10
— rather than porting directly from MVC4/.NET Framework 4.5 to .NET 10
in one effort.

**Alternatives considered:** A single-step ported rewrite, carrying the
domain model straight to ASP.NET Core in one pass.

**Reasoning:** A single-step port means changing framework, auth
mechanism, data access, and spatial data types all at once, with zero
existing test coverage as of the assessment. That's the highest-risk way
to do this work. Phase 1 stays within the same runtime, so it's a
materially lower-risk set of changes, and it's where the test safety net
gets established — which then de-risks Phase 2. The two-phase structure
also produces two independently verifiable outcomes instead of one
big-bang result, which is more honest evidence for the case study.

**Status:** Adopted. Originally proposed as a single-step ported rewrite
in the assessment's first draft; corrected after review (see assessment
commit `b2aa3ad`).

---

### DL-002 — .NET Framework 4.8.x as the Phase 1 landing point

**Decision:** Phase 1 targets .NET Framework 4.8.x, not an intermediate
.NET Framework version.

**Reasoning:** 4.8.x is the final, most current version of .NET
Framework and the natural landing point for an in-place upgrade — no
reason to stop at an intermediate version when the target runtime family
is being retired either way in Phase 2.

**Status:** Adopted.

---

### DL-003 — Strangler-fig via reverse proxy path routing for Phase 2

**Decision:** Phase 2 runs the legacy Framework app and the new ASP.NET
Core app as separate processes, routed by URL path through a reverse
proxy, migrated controller-by-controller.

**Alternatives considered:** A single cutover (stop the old app, deploy
the new one all at once).

**Reasoning:** The assessment initially concluded a literal strangler-fig
wasn't viable because MVC4/.NET Framework and ASP.NET Core don't share a
runtime for in-process side-by-side routing. That's true for *in-process*
strangler-fig, but a reverse-proxy-based strangler-fig (two separate
processes, routed at the HTTP layer) doesn't require a shared runtime —
it only requires both apps to be reachable behind a common entry point.
This became viable specifically because Phase 1 removes the
framework-currency and test-coverage blockers that would have made even
a proxy-routed incremental cutover risky. Sequencing migrates the
lowest-risk routes first (`Home`, `Search`) and the highest-risk (`Account`/
auth) last, so the pattern is proven before the hardest part is
attempted.

**Status:** Adopted. Corrects the assessment's original single-step
rewrite recommendation.

---

### DL-004 — Characterization tests before any modification

**Decision:** M2 (characterization test suite) is established against
the *original* pre-Phase-1 behavior, before M3's framework/dependency
upgrade begins — not written retroactively after changes are made.

**Reasoning:** The assessment's single most consequential finding was
zero automated test coverage (Category 8, score 1.0). Writing
characterization tests after changes have already been made would only
verify the new behavior against itself, not against the truth of what
the system actually did beforehand. Tests must capture current behavior
first, including known-fragile behavior (e.g., `GeolocationService`'s
unhandled failure paths), even where that behavior is bad — the goal is
a truthful baseline, not a pre-emptive fix disguised as a test.

**Status:** Adopted.

---

### DL-005 — Category 10 (Business Criticality) left unscored; no
invented business context

**Decision:** The assessment does not assign scores to Category 10, and
this plan is not sequenced or prioritized by business urgency.

**Reasoning:** There's no real business behind this codebase. An earlier
draft of the assessment used an invented business context to produce a
Category 10 score — this was corrected after review because a fabricated
justification undermines the credibility of the rest of the assessment.
Practical consequence for this plan: milestones are sequenced by
technical risk and dependency order (safety net before changes, low-risk
before high-risk migrations), not by a business-driven priority ranking
that doesn't actually exist.

**Status:** Adopted. See `assessment.md` commit `b2aa3ad`.

---

### DL-006 — Auth stack replacement timing is conditional on M1

**Decision:** Replace SimpleMembership/DotNetOpenAuth with ASP.NET
Identity + OWIN external-login middleware during Phase 1 (M4), not
deferred to Phase 2.

**Reasoning:** M1's research found DotNetOpenAuth has no maintained fork
and no supported upgrade path — it's dead regardless of runtime. The
replacement path (ASP.NET Identity 2.x + `Microsoft.Owin.Security.*`,
currently 4.2.3) is actively maintained and explicitly supports .NET
Framework 4.8/4.8.1, so there's no need to wait for Phase 2's ASP.NET
Core Identity. Running a dead, unsupported auth library through the
entirety of Phase 1 was the riskier option once a supported alternative
was confirmed to exist on the same runtime.

**Status:** Resolved by M1. See `m1-dependency-research.md`.

---

### DL-007 — Mobile-specific views: dropped in Phase 1, responsive design deferred to Phase 2

**Decision:** Remove the mobile-specific views (`Index.Mobile.cshtml`,
`_Layout.Mobile.cshtml`) during Phase 1 rather than porting them.
Responsive design is deferred to Phase 2, folded into the `_Layout`
rewrite that the ASP.NET Core port requires regardless.

**Context:** Inspection found the mobile footprint is much smaller than
"a mobile version of the app" implies — exactly two files, covering only
the Home page. Every other page already has no mobile variant and falls
back to the desktop layout unconditionally. The mobile Home page
duplicates functionality (a location search box) already present on the
desktop Home page.

**Reasoning:** The question was whether the views need a rewrite
regardless of a mobile decision, which would make "build responsive
while rewriting" free, or whether they can be ported as-is, which would
make added responsive work pure incremental cost. Checking directly: for
Phase 1 (.NET Framework 4.8), the views port with only mechanical edits
(removing Modernizr/yepnope calls and jQuery Mobile markup, already
required by M1's dependency removals) — no rewrite trigger exists here.
For Phase 2 (ASP.NET Core), `_Layout.cshtml` **will** be rebuilt
regardless, since `System.Web.Optimization` bundling doesn't exist in
ASP.NET Core and the script/style wiring has to be redone as part of the
port itself. That's the actual rewrite trigger, and it lands in Phase 2,
not Phase 1. Building responsive design once, at that point, avoids
doing it twice and avoids maintaining a jQuery-Mobile-specific page
through Phase 1 that's scheduled for replacement anyway.

**Consequence:** Mobile visitors see the (already viewport-tagged)
desktop layout during Phase 1 and early Phase 2, rather than a
dedicated mobile experience or a broken one — a reasonable interim
degradation, not a regression, since the mobile page offered no distinct
functionality.

**Status:** Adopted.

---

### DL-008 — M3 verified by actually building and running the suite, not by inspection

**Decision:** M3 was executed and verified in an environment with a real
MSBuild toolchain (Visual Studio Build Tools), `nuget.exe` with live
`nuget.org` access, and a LocalDB instance — every package upgrade,
namespace fix, and config change in this milestone was compiled and the
full characterization suite was run before and after, not just
reasoned about from reading the code.

**Context:** `m2-characterization-tests.md` documented that M2 had to be
authored blind, with no .NET SDK, no NuGet access, and no way to compile
or run the suite in-session — verification happened separately, by the
user, in Visual Studio. That limitation did not hold for M3: this
session had `MSBuild.exe`, `nuget.exe`, and `SqlLocalDB.exe` available
and used them directly (baseline: 68 passed / 2 skipped, matching M2's
documented baseline exactly; same result after every subsequent change
in this milestone).

**Reasoning:** Recording this because it changes the verification story
for the remaining Phase 1 milestones — M4 through M6 can be built and
tested the same way in-session, not just authored and handed off for
manual VS verification like M2 was. Worth knowing before assuming the
M2-era limitation still applies.

**Status:** Adopted.

---

### DL-009 — M1's specific target versions superseded by what NuGet actually had available at execution time

**Decision:** Where M1's recorded target version differed from the
newest version actually available on `nuget.org` when M3 ran, the live
listing won, not the number written down in
`m1-dependency-research.md`. Three cases:

- **jQuery**: M1 recorded a target of 4.0.0. The `jQuery` NuGet package
  (as opposed to the npm/CDN distribution) tops out at **3.7.1** — no
  4.x release exists on NuGet as of this milestone. Used 3.7.1.
- **PagedList replacement**: M1 recorded "replace now with `X.PagedList`
  ... current release 8.x for classic MVC/`System.Web`." By the time
  M3 ran, `X.PagedList` had moved to a 10.x line that dropped .NET
  Framework support entirely (netstandard2.0/net6.0/net8.0 only, no
  net45/net46x `lib` folder). The newest version that still ships a
  classic-framework build is **7.9.0** (net45/net461); versions 8.0.5
  and later are netstandard2.0+-only. Used 7.9.0.
- **Microsoft.SqlServer.Types**: M1 flagged this "upgrade in-place"
  without naming a specific target version. Chose **14.0.1016.290**
  (the SQL Server 2017-era release) over the newest available
  (170.1000.7, SQL Server 2022-era) — it's the most widely-adopted
  version in the package's install history and a smaller version jump
  off the 10.50 baseline, given this whole dependency is superseded
  outright by NetTopologySuite in Phase 2 regardless (per M1).

**Reasoning:** M1's research is dated (checked against nuget.org in
2026, per its own header), but "checked directly rather than assumed"
was the standard it set for itself — the same standard applies when
executing the plan later and reality has moved since the note was
written. Re-verifying at execution time and taking the live answer is
consistent with that, not a deviation from it. None of these changes
affect the plan's Phase 1/Phase 2 boundary or any decision already
logged.

**Status:** Adopted.

---

### DL-010 — EF5→EF6 required source fixes beyond the package bump; done as part of M3, not treated as a behavior change

**Decision:** Three fixes were required to make the app actually
function on EntityFramework 6.5.2, beyond changing the package version.
All three were made as part of M3 rather than logged as accepted
regressions, because none of them change observable behavior — they
restore the pre-upgrade behavior under the new package. The
characterization suite's pass/skip counts are identical before and
after (68 passed / 2 skipped both times).

- **`System.Data.Spatial.DbGeography` → `System.Data.Entity.Spatial.DbGeography`.**
  EF6 moved the spatial types out of the in-box `System.Data.Entity`
  assembly and into the `EntityFramework` NuGet package itself, under a
  new namespace. The app's EF5-era `using System.Data.Spatial;` left
  `DbGeography` on `Dinner.Location` pointing at a type EF6's model
  builder no longer recognizes as a spatial primitive — it tried to map
  it as a keyless entity instead, and `DbModelBuilder.Build` threw a
  `ModelValidationException` on every DB-backed test. Fixed in
  `Dinner.cs`, `SearchController.cs`, `DbGeographyModelBinder.cs`, both
  `DbGeography.cshtml` templates, and the test project's
  `TestDatabase.cs`.
- **`EntityState` namespace ambiguity.** `DinnersController.cs` has both
  `using System.Data;` (the old `EntityState`) and
  `using System.Data.Entity;` (EF6's own `EntityState`) — EF6
  introducing its own copy of that enum made the unqualified reference
  in `Edit()` ambiguous. Fully qualified it as
  `System.Data.Entity.EntityState.Modified`.
- **`Microsoft.SqlServer.Types` native binary loading.** The 10.50-era
  package auto-registered its native spatial DLL; the 14.0-era package
  does not — it ships `nativeBinaries\{x86,x64}\SqlServerSpatial140.dll`
  plus a `SqlServerTypes.Utilities.LoadNativeAssemblies(...)` helper
  (`content\SqlServerTypes\Loader.cs`) that the consuming app must call
  explicitly before touching any `DbGeography`/`SqlGeography` value.
  This is normally wired up by the package's `install.ps1` inside
  Visual Studio's NuGet integration; `nuget.exe` run from the command
  line (as this session did) does not execute that script, so it had to
  be done by hand: `Loader.cs` added to `src/SqlServerTypes/`, the
  native DLLs copied into `SqlServerTypes\x86\` and `SqlServerTypes\x64\`
  under both the app and test projects (each process resolves the path
  relative to its own base directory), and
  `SqlServerTypes.Utilities.LoadNativeAssemblies(...)` called once from
  `Global.asax.cs` `Application_Start` (app) and once from
  `TestDatabaseFixture`'s constructor (tests) — before either touches
  the database.

**Reasoning:** These aren't optional cleanup — without all three, the
app doesn't run and the DB-backed half of the characterization suite
fails outright. They restore identical behavior to the EF5 baseline
under EF6, which is exactly what "upgrade in-place" is supposed to mean
for this dependency. None of it is a deferred decision or an accepted
regression, so none of it needed the "update the test deliberately"
treatment DL-004 reserves for actual behavior changes.

**Status:** Adopted.

---

### DL-011 — MVC5 `ProjectTypeGuid` restored after MSBuild silently dropped it

**Decision:** `src/NerdDinner.csproj`'s `ProjectTypeGuids` now includes
`{E53F8FEA-EAE0-44A6-8774-FFD645390401}` (the ASP.NET MVC 5 project-type
marker) in place of the MVC 4 marker
(`{E3E379DF-F4C6-4180-9B81-6769533ABE47}`) it had before.

**Context:** During M3's build verification, MSBuild rewrote the
`.csproj` on disk mid-build (adding empty `Use64BitIISExpress` /
`UseGlobalApplicationHostFile` elements) and, in the process, silently
dropped the MVC4 GUID entirely rather than updating it — leaving the
project typed as a bare web application with no MVC marker at all. This
wasn't a change this session made intentionally; it surfaced by
diffing the file after a build. Left alone, it doesn't break compiling
or the test suite (Visual Studio scaffolding features like "Add View"
are the only thing that read this GUID), but it's exactly the marker
that Microsoft's own MVC4-to-MVC5 upgrade guide says to swap, so it's
worth setting correctly rather than leaving it silently absent.

**Reasoning:** Recorded because it's a real, if minor, gotcha for
anyone repeating this upgrade path with newer MSBuild tooling: don't
assume the `.csproj` is inert just because MSBuild's job is to read it,
not write it.

**Status:** Adopted.

---

### DL-012 — `Views/Web.config` needed the same version bump as the root config, missed on the first pass

**Decision:** `src/Views/Web.config` now pins `System.Web.WebPages.Razor`
at `Version=3.0.0.0` (was `2.0.0.0`) and `System.Web.Mvc` at
`Version=5.3.0.0` (was `4.0.0.0`) in its `configSections` declarations
and `<pages>`/`<system.web.webPages.razor>` type strings.

**Context:** Found only after the user launched the app in Visual
Studio and hit a parser error at the browser:
`Could not load file or assembly 'System.Web.WebPages.Razor,
Version=2.0.0.0, ...'. The located assembly's manifest definition does
not match the assembly reference.` `Views/Web.config` is a separate
config file from the site's root `Web.config`, read specifically by the
Razor view engine, and it hardcodes strong-name version numbers
directly in five places rather than going through the
`<runtime><assemblyBinding>` redirects that fixed the equivalent problem
in the root config during M3. Missed entirely on the first pass because
nothing in the automated safety net exercises it: the characterization
suite calls controller actions directly and inspects the returned
`ViewResult`/model without ever asking ASP.NET to actually parse and
compile a `.cshtml` file, which is the only code path that reads this
file.

**Reasoning:** This is a known, explicit step in Microsoft's own
MVC4-to-MVC5 upgrade guide (update `Views/Web.config` alongside the
project's package references) that got missed here specifically because
nothing runs the real ASP.NET pipeline in this session — a genuine blind
spot in characterization-test coverage for this kind of change, not a
new decision. Recording it as a concrete instance of "assessment/testing
by exercising code finds different things than assessment by reading
it" (the same theme M2 called out for the `ws.geonames.org` retirement),
and as a reminder that config-file version pins outside the root
`Web.config` need the same audit as package references whenever MVC (or
WebPages/Razor) moves major version again.

**Status:** Adopted.

---

### DL-013 — GeoNames username sourced from the local user-secrets store via `Microsoft.Configuration.ConfigurationBuilders.UserSecrets`

**Decision:** `GeolocationService.PlaceOrZipToLatLong` reads its
required GeoNames username from `ConfigurationManager.AppSettings["GeoNames:UserName"]`,
same as every other externalized setting in this app
(`ipInfoDbKey`, `BingMapsKey`, etc.). The value itself is never checked
into the repo: `src/Web.config`'s `<appSettings>` declares
`configBuilders="Secrets"`, wired to a `Microsoft.Configuration.ConfigurationBuilders.UserSecretsConfigBuilder`
(`Microsoft.Configuration.ConfigurationBuilders.Base` +
`.UserSecrets`, both 3.0.0) pointed at a `userSecretsId`
(`4c9de86e-4e70-4bd0-9d80-43532a0c4284`). At runtime, that builder reads
`%APPDATA%\Microsoft\UserSecrets\{that-id}\secrets.json` and merges any
matching keys into `AppSettings` transparently, before application code
ever sees it — `GeolocationService` doesn't know or care where the value
came from. `NerdDinner.Tests/App.config` got the identical
`configBuilders`/`userSecretsId` wiring (plus the two
`Microsoft.Configuration.ConfigurationBuilders.*` package references)
so the Integration-tagged `GeolocationServiceTests` can pick up the same
locally-stored secret rather than fail with an unhelpful
`ArgumentNullException` from `Uri.EscapeDataString(null)`.

**Supersedes:** an earlier version of this decision (logged, then
removed from this file during further work rather than left standing)
had `GeolocationService` calling a hand-rolled `Helpers/UserSecrets.cs`
reader instead — a ~20-line class that opened and parsed the same
`secrets.json` file directly with Newtonsoft.Json. Replaced because the
config-builder approach is the standard first-party mechanism for
exactly this (Microsoft ships it for precisely "read user-secrets into
classic `ConfigurationManager`-based apps"), and it means
`GeolocationService` keeps using the same `ConfigurationManager.AppSettings`
pattern as the rest of the codebase instead of a one-off custom
lookup path that only this one setting used.

**A gotcha that carries over from the superseded approach:** `dotnet
user-secrets set`/`list`/`remove` still cannot load `NerdDinner.csproj`
directly (`Could not load the MSBuild project ...`) since it's a
classic, non-SDK-style project file — this is unrelated to which
in-app mechanism reads the resulting file, so it applies here too. Use
the `--id <guid>` form, which talks to the secrets file directly and
skips project resolution entirely:

```
dotnet user-secrets set "GeoNames:UserName" "<your-username>" --id 4c9de86e-4e70-4bd0-9d80-43532a0c4284
```

One difference from the superseded approach worth noting: because the
config-builder reads the `userSecretsId` directly out of `Web.config`/
`App.config` rather than from a `<UserSecretsId>` MSBuild property, this
mechanism never needed `dotnet user-secrets init` or a project-level
`<UserSecretsId>` at all — only `set`/`list`/`remove` with `--id` are
relevant now.

**Status:** Adopted.

---

### DL-014 — Auth stack replaced: SimpleMembership + DotNetOpenAuth → ASP.NET Identity 2.2.4 + OWIN 4.2.3

**Decision:** M4 executed the replacement path M1 determined (resolved
by DL-006): `WebSecurity`/`OAuthWebSecurity`/DotNetOpenAuth are gone.
`AccountController` now runs on `Microsoft.AspNet.Identity.Core` +
`.EntityFramework` + `.Owin` 2.2.4, with `Microsoft.Owin.Host.SystemWeb`
+ `Microsoft.Owin.Security.{Cookies,MicrosoftAccount,Twitter,Facebook,Google}`
4.2.3 for cookie auth and the four external login providers — the exact
versions M1 predicted, still current when M4 ran (unlike the version
drift DL-009 had to work around in M3).

**What changed, file by file:**
- `Models/IdentityModels.cs` (new): `ApplicationUser : IdentityUser`,
  `ApplicationDbContext : IdentityDbContext<ApplicationUser>` against
  the existing `DefaultConnection` connection string — the same
  database SimpleMembership's `UserProfile` table used to live in, now
  holding Identity's standard `AspNetUsers`/`AspNetRoles`/etc. schema
  instead. No migration of old `UserProfile` rows: there's no real user
  data in this practice project (DL-005), so a fresh schema is strictly
  simpler than reverse-engineering a migration for data that doesn't
  exist.
- `App_Start/IdentityConfig.cs` (new): `ApplicationUserManager` /
  `ApplicationSignInManager`, with `UserValidator`/`PasswordValidator`
  explicitly configured to match SimpleMembership's old, looser policy
  (no character-class requirements, length ≥ 6 only, any user name) —
  Identity's defaults are stricter, and matching the old policy
  explicitly avoids silently tightening registration rules as a side
  effect of this milestone.
- `App_Start/Startup.cs` + `Startup.Auth.cs` (new): standard OWIN
  bootstrap (`[assembly: OwinStartup]`), cookie auth configuration, and
  the four external-provider registrations — each still conditional on
  its config keys being non-empty, same externalized-secrets pattern as
  the old `AuthConfig.cs` it replaces (deleted).
- `Controllers/AccountController.cs`: fully rewritten against
  `UserManager`/`SignInManager` instead of `WebSecurity`/
  `OAuthWebSecurity`, but every action name, route, and view model shape
  (`LoginModel`, `RegisterModel`, `LocalPasswordModel`,
  `RegisterExternalLoginModel`, `ExternalLogin`) is unchanged — existing
  views needed no changes except `_ExternalLoginsListPartial.cshtml`
  (see below). `[InitializeSimpleMembership]` and its attribute class
  (`Filters/InitializeSimpleMembershipAttribute.cs`, deleted) are gone;
  Identity's own `Database.SetInitializer` handles first-run schema
  creation the same way SimpleMembership's initializer did.
- `Views/Account/_ExternalLoginsListPartial.cshtml`: the one view
  requiring a real change. It modeled itself on
  `AuthenticationClientData` (DotNetOpenAuth's helper type, gone); now
  uses OWIN's `AuthenticationDescription` (`AuthenticationType` in place
  of `ProviderName`, `Caption` in place of `DisplayName`). Every other
  Account view compiles and behaves unchanged.
- `Models/AccountModels.cs`: `UsersContext`/`UserProfile`
  (SimpleMembership-specific) removed; the view models are untouched.
- `Web.config`: `<authentication mode="Forms">` → `mode="None"` — OWIN's
  cookie middleware (configured in `Startup.Auth.cs`) replaces Forms
  Authentication's role entirely, and leaving both active risks the two
  competing over 401 handling and the login redirect. The
  `dotNetOpenAuth` config section, its `<dotNetOpenAuth>` element, and
  the now-unnecessary `<uri>` IDN/RFC3986 section (needed only for
  OpenID/OAuth unicode domain handling) are removed. Added
  `googleClientId`/`googleClientSecret` app settings (see below).

**Observable behavior changes** (per M4's acceptance criteria, each
justified rather than silently absorbed):
- **Google login now requires configuration.** The old
  `OAuthWebSecurity.RegisterGoogleClient()` used a keyless OpenID 2.0
  flow; OWIN's Google provider is OAuth 2.0-only and requires a
  registered `ClientId`/`ClientSecret`, same as the other three
  providers. This is a smaller change than it sounds: Google retired
  OpenID 2.0 in 2015, so the old keyless flow was already dead in
  practice, not a working feature being removed.
- **Session cookie changes identity/format.** OWIN's
  `ApplicationCookie` replaces the ASP.NET Forms Authentication ticket
  cookie SimpleMembership rode on top of — different cookie name,
  different payload format. Anyone with an existing session is signed
  out once; there's no session data worth preserving across that
  boundary in this practice project.
- **Registration failure messages are Identity's own strings**, not the
  old `MembershipCreateStatus`-keyed messages from
  `ErrorCodeToString()` (removed) — e.g. Identity's own duplicate-name
  and validation error text, surfaced via `IdentityResult.Errors`
  instead. Still error text describing the same underlying conditions
  (duplicate name, invalid password), just not byte-for-byte identical
  wording.

**Testing:** `AccountControllerTests.cs`'s own comment (written in M2)
promised that the observable authentication contract would get
characterized once M4 landed a real seam. It does now:
`AccountControllerIdentityTests` exercises `ApplicationUserManager`
directly against a dedicated `NerdDinnerIdentityTests` LocalDB database
(`IdentityTestDatabaseFixture`, mirroring `TestDatabaseFixture`'s
pattern but kept as a separate database from `NerdDinnerContext`'s
Dinners/RSVPs data so the two fixtures' drop/create lifecycles can't
interfere with each other) — registration success, duplicate-name
rejection, password-length rejection, and correct/incorrect-password
login all pass. Action-level flows that need a live OWIN context
(`ExternalLogin` challenge/callback, `Manage`, `Disassociate`) remain
untested at the controller level, same limitation `AccountControllerTests`
already had pre-M4 for a different reason (ambient static state instead
of ambient OWIN context) — still a real, documented gap, not a
regression from where M2 left it.

**Status:** Adopted.

---

### DL-015 — `DbGeographyModelBinder` array-index bug fixed (pre-existing since the 2012 baseline, unrelated to M3/M4)

**Decision:** `DbGeographyModelBinder.BindModel` now checks that the
posted "lat,long" value actually splits into two non-empty parts before
building a `DbGeography` from it, returning `null` otherwise. Previously
it unconditionally indexed `latLongStr[1]`, which threw
`IndexOutOfRangeException` for any posted value without a comma.

**Context:** Found by the user manually testing Create Dinner in the
browser after M4 — reasonable to suspect at first, since M3 touched
this exact file's `DbGeography` namespace. Checked git history to be
sure: the only change ever made to this file is the one-line
`System.Data.Spatial` → `System.Data.Entity.Spatial` namespace fix from
M3 (DL-010), a type substitution with zero logic change. The array-index
bug is byte-for-byte what shipped in the original 2012 baseline import
(commit `bf314f5`) — confirmed by diffing against it directly, not
assumed.

**Root cause:** `Views/Shared/EditorTemplates/DbGeography.cshtml` posts
`Location=""` whenever nothing has been geocoded yet (its `else`
branch, hit on every fresh Create). The field only gets populated by
`NerdDinner._callbackForLocation` in `NerdDinner.js`, which depends on
a successful Bing Maps geocoding round trip keyed by `BingMapsKey` — an
app setting that ships blank in the checked-in `Web.config`, same
"externalized but not configured" pattern as `ipInfoDbKey` and (until
DL-013) `GeoNames:UserName`. Without a real key, `Location` never gets
set, and `"".Split(',')` yields a one-element array.

**Reasoning:** `Dinner.Location` has no `[Required]` attribute — the
model already permits a dinner with no location (`SearchController`'s
characterized `JsonDinnerFromDinner` NRE-on-null-Location bug depends on
exactly this being legal). Returning `null` for an empty/malformed
posted value is consistent with that existing contract, not a new
relaxation of it. Chose to fix immediately rather than defer to M5, per
explicit direction — this is the same "no fallback / no validation
before touching untrusted input" pattern the original assessment
already flagged for `GeolocationService` (Category 7), just in a
different file, so fixing it doesn't expand scope conceptually even
though `DbGeographyModelBinder.cs` wasn't named in that finding.

**Testing:** `NerdDinner.Tests/ModelBinders/DbGeographyModelBinderTests.cs`
(new) characterizes all four cases directly against the binder: empty
posted value, field not posted at all, malformed value with no comma
(all → `null`), and a well-formed pair (→ correct `DbGeography`). Also
fixed, in the same pass: `TestSupport/IdentityTestDatabase.cs` (added
during M4, DL-014) was never actually added to
`NerdDinner.Tests.csproj` — it silently didn't compile, and the
Identity tests only passed because EF6's default
`CreateDatabaseIfNotExists` initializer stepped in regardless. Both the
new binder test file and the missing M4 file are now correctly wired
into the project.

**Status:** Adopted.
