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

---

### DL-016 — M5 security/config hardening: HTTPS everywhere, no more silent failures in `GeolocationService`

**Decision:** All of M5's acceptance criteria landed together:

- `Web.config`: `<customErrors mode="Off">` → `mode="RemoteOnly"`.
  `compilation debug="false"` for Release was already handled by the
  pre-existing (unmodified since the 2012 baseline) `Web.Release.config`
  transform (`RemoveAttributes(debug)`, which defaults `debug` to
  `false` when absent) — confirmed present and correct, nothing to add.
- `GeolocationService.PlaceOrZipToLatLong`: geonames.org call moved to
  HTTPS. `GeolocationService.HostIpToPlaceName`: ipinfodb.com call moved
  to HTTPS.
- `_Layout.cshtml`'s Bing Maps `<script>` tag moved to HTTPS.
- The hardcoded fallback IP literal (`"71.117.141.83"`, substituted for
  `127.0.0.1` in `HostIpToPlaceName`) is removed outright.
- Both `GeolocationService` methods now wrap their external HTTP call in
  try/catch and return `null` on any failure, instead of letting
  `XDocument.Load`/`.First()` throw unhandled.

**A real, non-obvious finding while doing the HTTPS switch:**
`api.geonames.org` does not serve a valid certificate for itself on
port 443 — `curl`/schannel fail with `SEC_E_WRONG_PRINCIPAL` (SNI/cert
hostname mismatch). Checked what certificate is actually being served:
`CN=secure.geonames.org`. GeoNames requires a **different hostname**
for HTTPS than for HTTP, not just a protocol change on the same host —
confirmed by successfully querying `https://secure.geonames.org/postalCodeSearch?...`
directly. `PlaceOrZipToLatLong` now points at `secure.geonames.org`,
not `api.geonames.org`, for its HTTPS request. This is the same shape
of gotcha as the `ws.geonames.org` → `api.geonames.org` retirement M2
found (DL-009/M3): don't assume a same-host protocol swap works, verify
it directly. The Bing Maps and ipinfodb.com HTTPS switches, by contrast,
needed no hostname change — checked each directly rather than assuming
they'd all behave the same way.

**Verified against the live APIs**, not just reasoned about:
`PlaceOrZipToLatLong_ReturnsCoordinates_ForKnownValidZip` (real
coordinates, over HTTPS, with the tester's own registered GeoNames
username per DL-013), `PlaceOrZipToLatLong_ReturnsNull_WhenNoResultsFound`,
and the updated `HostIpToPlaceName_ReturnsNull_WhenApiKeyIsBlank` all
pass against the live, HTTPS endpoints.

**Deliberate behavior change, per plan.md's own instruction for this
milestone:** `HostIpToPlaceName` used to propagate whatever exception a
missing `ipInfoDbKey` produced straight to the caller; it now returns
`null`, matching `PlaceOrZipToLatLong`'s existing "no match" contract.
`GeolocationServiceTests.HostIpToPlaceName_ThrowsUnhandledException_WhenApiKeyIsBlank`
is renamed to `..._ReturnsNull_WhenApiKeyIsBlank` and its assertion
updated accordingly, with a comment pointing at this entry rather than
silently changing what it checks.

**Originally left out of scope, addressed immediately after per explicit
direction:** `NerdDinner.js` made several of its own plain-HTTP calls to
`dev.virtualearth.net`'s REST geocoding endpoints and `api.ipinfodb.com`
— the same insecure-transport pattern as everything above, but not
named in M5's acceptance criteria (which specifically scoped the Bing
Maps fix to the `_Layout.cshtml` script tag). See DL-017 for the fix.

**Status:** Adopted.

---

### DL-017 — `NerdDinner.js`'s remaining plain-HTTP calls moved to HTTPS

**Decision:** All four plain-HTTP URLs in `Scripts/NerdDinner.js` are
now HTTPS: the two `dev.virtualearth.net/REST/v1/Locations` forward-geocode
calls (`FindAddressOnMap`, `FindDinnersGivenLocation`), the reverse-geocode
call (`getCurrentLocationByLatLong`), and the client-side ipinfodb.com
call (`getCurrentLocationByIpAddress`).

**Context:** DL-016 explicitly left these out of M5's scope, since
plan.md's acceptance criteria named only the `_Layout.cshtml` Bing Maps
`<script>` tag. Brought back in immediately afterward per explicit
direction — "security hardening is what M5 is about" — rather than
waiting for a later milestone.

**Verified before changing, same discipline as DL-016:**
`https://dev.virtualearth.net/REST/v1/Locations` (both the query and
`{lat},{lng}` forms) returns `401` over HTTPS with a placeholder key —
a real TLS handshake and server-side processing, not a connection-level
failure, confirming no hostname change is needed here (unlike GeoNames).
`https://api.ipinfodb.com` was already confirmed working in DL-016.

**Deliberately left alone:** the social-share and attribution links in
`Views/Dinners/Details.cshtml`, `WebSlice.cshtml`, `Home/About.cshtml`,
and `_Layout.cshtml`'s footer (Twitter/Facebook/Google Reader share
intents, credit links to contributors' personal sites, the defunct
`nerddinner.codeplex.com`) also use plain `http://`. These are
outbound links a user chooses to click, not application-initiated
requests carrying API keys or user data — a different risk category
from a script-tag/XHR call the app makes automatically, and several
point at third-party sites that may not even serve HTTPS. Not part of
this pass.

**Status:** Adopted.

---

### DL-018 — M6: checked-in `.mdf`/`.ldf` files removed, EF6 Migrations reproduces schema and seed data instead

**Decision:** The four checked-in LocalDB files under `src/App_Data/`
(`aspnet-NerdDinner-*.mdf`/`.ldf`, `NerdDinnerContext-*.mdf`/`.ldf`) are
deleted from the working tree (no history rewrite of the baseline
import commit — they're still recoverable from `bf314f5` if ever
needed). Both `Web.config` connection strings (`DefaultConnection`,
`NerdDinnerContext`) switched from `AttachDbFilename=|DataDirectory|\...`
to named LocalDB databases (`Initial Catalog=NerdDinner.Identity` /
`NerdDinner`) that get created fresh on first use. `.gitignore` gained
`*.mdf`/`*.ldf`/`*.ndf` so these can't silently come back.

**Schema/seed mechanism:**
- `NerdDinnerContext` (Dinners/RSVPs): real EF6 Migrations —
  `Migrations/Configuration.cs` + a scaffolded `InitialCreate` migration
  (`Migrations/202608272110081_InitialCreate.cs`), wired up via
  `Database.SetInitializer(new MigrateDatabaseToLatestVersion<NerdDinnerContext, Configuration>())`
  in `Global.asax.cs` `Application_Start`. `Configuration.Seed()`
  populates two clearly-fictional placeholder dinners (Seattle,
  Portland) — explicitly **not** an attempt to reproduce the deleted
  `.mdf`'s actual contents, which was itself flagged in the original
  assessment (Category 4) as ad hoc, unreproducible data no test or
  fixture should depend on.
- `ApplicationDbContext` (Identity, `DefaultConnection`): left on its
  existing automatic `CreateDatabaseIfNotExists` initializer from M4 —
  no formal migrations, since Identity's schema needs no seed data and
  a fresh `AspNetUsers`/etc. schema is all that's required.

**How the `InitialCreate` migration was actually generated:** `Add-Migration`
is a PowerShell cmdlet with no CLI equivalent, unusable outside a VS
Package Manager Console session. Scaffolded it programmatically instead,
via the same public API the cmdlet wraps
(`System.Data.Entity.Migrations.Design.MigrationScaffolder`), from a
throwaway console harness referencing the built `NerdDinner.dll` (`Configuration`
temporarily made `public` for the harness to construct it, reverted to
`internal` immediately after — the real app only ever needs `internal`
access, same assembly). Chose this over hand-writing the migration
because `DbGeography` columns have SQL-Server-specific spatial DDL
conventions (`c.Geography()`) not worth risking a hand-written mistake
on when the real scaffolder produces a materially different,
provider-correct result automatically.

**Verified, not just written:** a second throwaway harness pointed a
fresh, disposable LocalDB catalog (`NerdDinnerMigrationVerify`) at the
real `MigrateDatabaseToLatestVersion<NerdDinnerContext, Configuration>()`
initializer end to end — confirmed the migration creates the schema
(including a working `DbGeography` column: seeded coordinates round-tripped
correctly), `Seed()` populates exactly the two placeholder dinners, and
calling `Database.Initialize` a second time (simulating a later app
start against an already-migrated database) is a no-op rather than a
duplicate-seed or migration-history error. Both throwaway harnesses were
deleted after use; nothing from them is checked in.

**Not covered by the automated test suite:** `NerdDinner.Tests`'s own
`TestDatabaseFixture` manages `NerdDinnerContext` with its own
`DropCreateDatabaseAlways` initializer (a deliberate M2 choice — tests
need a wipe-and-reseed-per-run database, not migration history), so it
never exercises the `Migrations` mechanism at all. Adding a permanent
xUnit test for the migration path was considered and rejected:
`Database.SetInitializer<NerdDinnerContext>` is static, per-AppDomain
global state, so a test that set the Migrations initializer would
silently override whichever initializer runs for every other
`NerdDinnerContext` test in the same process depending on execution
order — a real risk of flaky, order-dependent test pollution for a
verification that a one-time manual run already covers thoroughly.

**Status:** Adopted.

---

### DL-019 — Glimpse removal explicitly assigned to M11, not left implicit

**Decision:** `plan.md` M11 (Decommission legacy app) now names Glimpse
removal explicitly in its acceptance criteria, rather than leaving it
to happen implicitly when the legacy Framework app is deleted wholesale.

**Context:** The Phase 1 exit checkpoint
(`docs/03-outcome/phase-1-exit-checkpoint.md`) flagged Glimpse as a
residual finding: M1 disposed of it as "remove entirely" back when
dependencies were first researched, but that disposition was never
actually assigned to any Phase 1 milestone's acceptance criteria, so it
never got removed — the exact "flagged once, then quietly dropped"
failure mode this entry exists to prevent from happening a second time.
It would technically disappear on its own once M11 deletes the legacy
app entirely, but leaving it implicit is how it got missed the first
time.

**Status:** Adopted.

---

### DL-020 — M7 reverse proxy scaffold: YARP, catch-all to legacy, new app handles no business routes yet

**Decision:** `src-core/NerdDinner.Proxy` is a new ASP.NET Core (.NET 10)
minimal-API project, added to `NerdDinner.sln` alongside `src\NerdDinner.csproj`
(SDK-style and classic-style projects coexist in the same solution file
without issue). It references `Yarp.ReverseProxy` 2.3.0, configured via
`appsettings.json`'s `ReverseProxy` section rather than code: a single
catch-all route (`/{**catch-all}`) forwards every request to a
`legacyCluster` destination pointed at the legacy app's IIS Express
binding (`http://localhost:10581/`, per `src\Properties\PublishProfiles`'s
`IISUrl` and `.vs\NerdDinner\config\applicationhost.config`'s `NerdDinner`
site binding — checked directly rather than assumed). The new app's only
locally-handled endpoint is `GET /_proxy/health`, a diagnostic check
proving the new app itself is reachable through the proxy — explicitly
not a migrated business route, so it doesn't compromise M7's "new app
handles no routes" acceptance criterion. No route to the new app exists
for any real NerdDinner path yet; that starts at M8.

**Verified live, not just built:** both apps were actually started
(legacy app via `iisexpress.exe` against the existing
`applicationhost.config`, new app via `dotnet run`) and exercised
through the single proxy entry point (`localhost:5021`) —
`GET /_proxy/health` returned the new app's own text,
`GET /` and `GET /Dinners` both returned `200` with the legacy app's
actual page title (`Nerd Dinner`), confirming YARP is really forwarding
to the legacy app rather than the config merely parsing without error.
Both processes were stopped after verification; nothing was left
running.

**Reasoning:** Config-driven YARP routes (over code-configured routes)
keep the routing table declarative and easy to extend milestone-by-
milestone (M8 adds `Home`/`Search` routes to a new cluster, M9 adds
`Dinners`/`RSVP`, etc. — each an addition to `appsettings.json`, not a
structural change to `Program.cs`). Putting the new project under
`src-core/` (a solution folder, not a physical parent of `src/`) keeps
it clearly separate from the Phase 1 legacy tree while living in the
same repo and solution, consistent with DL-003's two-separate-processes
approach.

**Status:** Adopted.

---

### DL-021 — M8 narrowed to `Home` only; `Search` moved to M9

**Decision:** `plan.md`'s M8 ("Migrate stateless/read-only routes") now
targets `Home` only. `Search` moves into M9, renamed "Migrate Dinners,
RSVP, and Search (CRUD + spatial data)".

**Context:** Starting M8's implementation surfaced a real gap in the
original milestone split: `plan.md` grouped `Home` and `Search` together
as "stateless/read-only," but `SearchController` isn't stateless in the
sense that matters here — it's a Web API controller (`ApiController`)
that queries `Dinner`/`RSVP` directly via `NerdDinnerContext`, including
a `DbGeography.Distance` spatial query (`FindByLocation`). That's
exactly the data-access concern M9 already owns and names explicitly in
its acceptance criteria ("EF Core data access replaces EF6;
`Dinner.Location` uses a NetTopologySuite-based spatial type in place of
`DbGeography`"). `Home`, by contrast, is genuinely stateless — no
database access at all, just a static message and an About page.

**Alternatives considered:**
1. Pull the EF Core + NetTopologySuite migration forward into M8, scoped
   to what `Search` needs (read-only queries), leaving M9 to add
   `Dinners`/`RSVP` write operations on an already-proven data layer.
2. Have the new `Search` controller shape/route correctly but internally
   call the legacy app's existing `api/Search` endpoint under the hood,
   deferring the real EF Core migration to M9.
3. (Chosen) Narrow M8 to `Home` only; move `Search` into M9 alongside
   `Dinners`/`RSVP`.

**Reasoning:** Option 1 works technically but splits the "introduce EF
Core + NetTopologySuite" decision across two milestones for no reason
other than the original grouping being by HTTP verb shape
(read-only/stateless) rather than by actual architectural concern
(data access technology). Option 2 avoids that but ships a throwaway
shim in M8 that gets deleted at M9 and doesn't prove the new app's own
data access works — worse verification value for no real benefit. Option
3 fixes the grouping at the source: `Home` truly has no data-layer
dependency and can land independently; `Search`'s only meaningful
migration work *is* the data layer, so it belongs with the milestone
that already does that work for `Dinners`/`RSVP`, verified in the same
pass rather than twice.

**Status:** Adopted.

---

### DL-022 — M8: `Home` ported to `NerdDinner.Proxy`; real proxy-level integration tests added

**Decision:** `HomeController` and its two views (`Index`, `About`) are
now implemented for real inside `src-core/NerdDinner.Proxy`, using
ASP.NET Core MVC conventions (Razor views, tag helpers where the target
controller exists in the new app, plain `href`s where it doesn't yet).
`Program.cs` registers `AddControllersWithViews()` and an explicit
conventional route constrained to `controller=Home` only, so no other
controller name can match it. Static assets it needs (`Site.css`, the
`Images` referenced by `Site.css`/the views, `favicon.ico`, `jquery`,
`knockout`, `NerdDinner.js`, `geo.js`/`geo-polyfill.js`) are copied
as-is into `wwwroot`, unchanged from `src/`.

**A real bug found and fixed while verifying this live, not just
built:** the first working version had *every* request — including
literal paths like `/Home/About` — silently forwarded to the legacy app
by YARP, never reaching the new `Home` controller at all. Caught only by
actually running both apps and diffing response markers (script
filenames, image casing, the footer version string), not by reading the
config. Root cause: YARP's config-loaded catch-all route
(`/{**catch-all}`) and the MVC conventional route both default to
`Order = 0`; ASP.NET Core's endpoint matcher groups candidates by
`Order` before applying template-specificity precedence, and with both
routes in the same `Order` group, the catch-all's literal
always-matches nature won every request instead of losing on
specificity to more literal templates the way an in-process-only MVC
route table would have. Fixed by giving the YARP route an explicit
`"Order": 1000` in `appsettings.json`'s `ReverseProxy:Routes:legacy-catchall`,
forcing the matcher to exhaust the (default-`Order`-0) MVC routes first
and fall back to YARP only when nothing else matches. Worth remembering
for M9/M10: every new route added to the new app needs to actually be
exercised end-to-end through the proxy before trusting it's not
silently falling through to the legacy cluster — config that "looks
right" isn't enough, per the same theme DL-012 and DL-016 already
established for the legacy app.

**Responsive design (DL-007's deferred item) needed no new CSS work:**
`Site.css`, carried over unchanged, already had a complete
`@media (max-width: 850px)` block (header, login, menu, layout, forms,
footer) — evidently written for the app's older mobile-web-support
effort and left in place after M3 removed the jQuery-Mobile-specific
views. The rebuilt `_Layout.cshtml` keeps the same markup structure
and the `<meta name="viewport">` tag that block targets, so DL-007's
promised responsive design is inherited, not authored fresh.

**A known, deliberate interim gap:** `_LoginPartial.cshtml` in the new
app always renders the logged-out state (`Register`/`Log in` links to
the legacy `/Account/*` routes) rather than reading real authentication
state. The new app doesn't share the legacy app's OWIN authentication
cookie — cross-app session handling during the transition is explicitly
M10's acceptance criterion ("Session handling across the proxy boundary
during the transition period... explicitly tested, not assumed"), not
M8's. This is a visible, honest degradation (a genuinely logged-in user
would see "Log in" on the new-app-served `Home` page) rather than a
silent one, matching the pattern DL-007 already set for the mobile-view
removal.

**Testing:** `src-core/NerdDinner.Proxy.Tests` (new project,
`Microsoft.AspNetCore.Mvc.Testing`) hosts the actual `NerdDinner.Proxy`
app via `WebApplicationFactory<Program>` — its real `Program.cs` and
`appsettings.json`, not a test-only substitute — and asserts against
genuine HTTP responses: `/` and `/Home/About` are confirmed served by
the new app (via a marker only its own `_Layout` renders), `/Dinners`
(not yet migrated) is confirmed still reaching the legacy app through
the same running proxy instance, and `/_proxy/health` confirms the
diagnostic endpoint from M7 still works. Tagged
`[Trait("Category", "Integration")]` and excluded from the default fast
run, same convention as this repo's existing GeoNames/ipinfodb
integration tests — it requires the legacy app to actually be running
under IIS Express on `localhost:10581`, a live external dependency the
test harness doesn't spin up itself. Run against a real, separately
started legacy app during this milestone: 4/4 passed.

**Status:** Adopted.

---

### DL-023 — Pre-existing bug: `TestDatabaseFixture`/`DbGeographyModelBinderTests` resolved the native SqlServerSpatial DLL path incorrectly under VS Test Explorer

**Decision:** `NerdDinner.Tests/TestSupport/TestDatabase.cs`
(`TestDatabaseFixture.ctor`) and
`NerdDinner.Tests/ModelBinders/DbGeographyModelBinderTests.cs` (static
ctor) now resolve the directory they pass to
`SqlServerTypes.Utilities.LoadNativeAssemblies` from
`Assembly.CodeBase` (parsed as a URI, then `.LocalPath`), not
`AppDomain.CurrentDomain.BaseDirectory`.

**Context:** Unrelated to M7/M8 — a pre-existing bug from M3 (DL-010),
surfaced by the user running the suite in Visual Studio and getting ~29
failures spread across every DB-backed test class, all with the same
underlying error (`Error loading msvcr120.dll (ErrorCode: 126)`) inside
`TestDatabaseFixture`'s constructor. Because that fixture is shared
across the whole "NerdDinner LocalDB collection" via `ICollectionFixture`,
one failure in its constructor takes down every test in every class that
depends on it at once — which is exactly the "spread across everything"
symptom reported, not 29 independent bugs.

**Root cause, found by the user attaching a debugger (not by this
session guessing):** `LoadNativeAssemblies` builds its DLL path from
whatever directory string it's given, per `Loader.cs`'s own doc comment
distinguishing "`Server.MapPath(".")` for ASP.NET, `AppDomain.CurrentDomain.BaseDirectory`
for desktop apps" — the test project is neither, and this call site
(added in M3, DL-010) used the desktop-app convention. Under Visual
Studio's IDE-hosted Test Explorer, `AppDomain.CurrentDomain.BaseDirectory`
does **not** resolve to the test assembly's own `bin\Debug` — the user's
debugger showed it resolving to
`C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\Extensions\TestPlatform\`,
the VSTest host's own install directory. `LoadNativeAssemblies` then
built a path like `...\TestPlatform\SqlServerTypes\x64\msvcr120.dll`,
which doesn't exist, and `LoadLibrary` correctly reported "module not
found."

**Two failed attempts before the real fix, recorded because they're
instructive:**
1. First fix attempt used `Assembly.Location` (the loaded assembly's
   on-disk path) instead of `AppDomain.CurrentDomain.BaseDirectory`.
   This fixed the exact case the user debugged, but a follow-up run via
   `vstest.console.exe` from the command line (not the VS IDE) *still*
   failed at the same line, now with a *third* different bad path.
   Diagnostic instrumentation added temporarily to the fixture (then
   removed) confirmed why: the VSTest xUnit adapter shadow-copies the
   test assembly to a temp cache
   (`...\Temp\<guid>\<guid>\assembly\dl3\...`), and `Assembly.Location`
   reflects that copy's location, not the real one — a different failure
   mode than the IDE case, but the same class of problem.
2. That same diagnostic instrumentation printed `Assembly.CodeBase` and
   `AppDomain.CurrentDomain.BaseDirectory` alongside `Assembly.Location`
   for direct comparison. Under the CLI `vstest.console.exe` run,
   `AppDomain.CurrentDomain.BaseDirectory` was actually *correct*
   (unlike the IDE case) — confirming neither `Location` nor
   `BaseDirectory` is reliable on its own across every way this suite
   gets run, and that the earlier "reproduces fine via CLI" verification
   (done before the user's VS-specific debugging) had been quietly
   relying on a coincidence, not a fix.
3. **What actually worked:** `Assembly.CodeBase` (a `file://` URI to the
   assembly's real, original path, deliberately unaffected by shadow
   copying) — confirmed correct in the same side-by-side diagnostic
   output, and different from both `Location` and `BaseDirectory` in
   that run. Re-verified with the diagnostic instrumentation removed:
   full suite (`Category!=Integration`, 80 tests) passes via CLI
   `vstest.console.exe`. Still needs confirmation from the user inside
   Visual Studio's own Test Explorer, since that's the environment the
   bug was originally reported in and CLI success alone was previously
   shown to be an unreliable signal for this exact class of bug.

**Reasoning:** `CodeBase` is the standard, documented escape hatch for
exactly this "where is my assembly *really* on disk" question when a
host might shadow-copy or otherwise relocate it — it isn't a workaround
specific to this repo. Applied identically to both affected call sites
(`Global.asax.cs`'s own call, added in M4/DL-014 for the running app,
already correctly used `Server.MapPath("~/")` and needed no change).

**Status:** Adopted, confirmed fixed inside Visual Studio's Test
Explorer specifically — the user's next run progressed past this exact
failure point (from `LoadNativeAssembly` at `TestDatabase.cs` line
26/38, to `Database.Initialize` at line 57), which only happens if the
native DLL genuinely loaded. See DL-024 for the different, second issue
that surfaced once this one was actually fixed.

---

### DL-024 — Same class of VS Test Explorer AppDomain issue, second symptom: `ConfigurationManager` couldn't find connection strings that were right there in the compiled config

**Decision:** `NerdDinnerContext` and `ApplicationDbContext` (both in
`src/Models/`) gained an additive constructor overload taking a raw
connection string directly
(`NerdDinnerContext(string connectionString) : base(connectionString)`,
`ApplicationDbContext(string connectionString) : base(connectionString, throwIfV1Schema: false)`),
alongside their existing parameterless constructors (unchanged, still
what the running app uses exclusively). A new
`NerdDinner.Tests/TestSupport/TestConnectionStrings.cs` (internal
static, cached) reads connection strings directly out of this test
assembly's own compiled `NerdDinner.Tests.dll.config` via
`ConfigurationManager.OpenMappedExeConfiguration`, using the same
`Assembly.CodeBase`-derived path technique DL-023 established. Every
`new NerdDinnerContext()` / `new ApplicationDbContext()` call site in
`NerdDinner.Tests` (12 total, across `TestDatabase.cs`,
`IdentityTestDatabase.cs`, `DinnersControllerTests.cs`,
`RSVPControllerTests.cs`, `SearchControllerTests.cs`,
`AccountControllerTests.cs`) now passes
`TestConnectionStrings.Get("NerdDinnerContext")` /
`TestConnectionStrings.Get("DefaultConnection")` explicitly instead.

**Context:** Fixing DL-023 (native DLL loading) let the user's next VS
Test Explorer run progress further, into a second, different failure at
the same fixture's `db.Database.Initialize(force: true)` call:
`No connection string named 'NerdDinnerContext' could be found in the
application config file`. Same underlying theme as DL-023 — an AppDomain
hosting mismatch specific to VS's IDE Test Explorer — but a different
mechanism: `NerdDinnerContext()`'s parameterless constructor
(`base("name=NerdDinnerContext")`) asks EF6 to resolve that name via
`ConfigurationManager.ConnectionStrings`, which reads whatever config
file the AppDomain's ambient `ConfigurationFile` setting points at.
Confirmed directly (not assumed) that the actual compiled
`NerdDinner.Tests\bin\Debug\NerdDinner.Tests.dll.config` file has the
right connection string sitting right there — the problem is purely
about which config file that AppDomain was actually pointed at, the same
class of problem DL-023 already diagnosed for native DLL paths.

**Alternative considered and rejected:** setting
`AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", correctPath)` early,
which forces `ConfigurationManager` to use a specific file — but only if
set *before* `ConfigurationManager` is touched anywhere in that
AppDomain for the first time. xUnit runs test collections in parallel by
default, and this repo's non-DB-backed tests (e.g.
`GeolocationServiceTests`, which reads `ConfigurationManager.AppSettings`
directly) aren't in either DB-backed collection — a genuine race where
an unrelated collection could touch and cache the wrong config before
the DB fixture's constructor ever got a chance to override it. Reading
the correct file directly via `OpenMappedExeConfiguration`, and passing
the resolved string straight into the `DbContext` constructor, has no
such timing dependency.

**Verified:** full suite (`Category!=Integration`, 80 tests) still
passes via CLI `vstest.console.exe` after this change, and the legacy
app (`src/NerdDinner.csproj`) still builds clean — the two new
constructor overloads are additive and don't change either class's
existing parameterless-constructor behavior, which is all the running
app ever uses.

**Status:** Adopted, confirmed inside Visual Studio's Test Explorer —
see DL-025 and DL-026 for the further symptoms of this same underlying
issue that surfaced once this fix was in place, and DL-026's closing
note for the user's full-suite confirmation.

---

### DL-025 — DL-024 wasn't enough: controllers under test construct their own `NerdDinnerContext` internally, unreachable from test code

**Decision:** `DinnersController`, `RSVPController`, and `SearchController`
(the three controllers with a hardcoded `private NerdDinnerContext db =
new NerdDinnerContext();` field) now each have an additional
constructor overload taking a `NerdDinnerContext` directly:

```csharp
private readonly NerdDinnerContext db;

public DinnersController() : this(new NerdDinnerContext())
{
}

public DinnersController(NerdDinnerContext context)
{
    db = context;
}
```

The parameterless constructor is unchanged in behavior — it still builds
a `NerdDinnerContext` the normal way and is all the running app ever
uses. `NerdDinner.Tests`' 23 call sites that construct these controllers
directly (`new DinnersController()`, etc.) now pass
`new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext"))`
explicitly through the new overload.

**Context:** DL-024 fixed every place *test code itself* constructed a
`NerdDinnerContext`/`ApplicationDbContext`, but the user's next VS Test
Explorer run still failed — this time inside `DinnersController.Delete`
itself (`InternalSet.Find` → `InternalContext.Initialize` →
`No connection string named 'NerdDinnerContext'...`). Root cause: these
controllers construct their own `db` field via a hardcoded field
initializer, and `NerdDinner.Tests` exercises them by instantiating the
controller class directly (the standard, well-established way to unit
test this MVC generation — see `ControllerTestHelpers.cs`'s own doc
comment) rather than through a live HTTP pipeline. That means the
controller's internal context construction happens inside the *test's*
AppDomain, not inside IIS/IIS Express where the app's own config
resolves fine — the exact same underlying AppDomain-config mismatch
DL-023/DL-024 diagnosed, just reachable through a code path neither of
those fixes could see or touch.

**Alternative considered:** fixing this globally via a C# 9 module
initializer in `NerdDinner.Tests` (with a small polyfill attribute, since
net48 doesn't ship `ModuleInitializerAttribute`) that calls
`AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", ...)` before any
other code in the test assembly runs — CLR module-load semantics
guarantee this runs before anything else in the module, sidestepping the
parallel-collection race DL-024 rejected for the same idea. Rejected in
favor of constructor injection: no production controller code needed,
but the fix would have been invisible/non-obvious to a future reader
(a module initializer touching global AppDomain state isn't something
most C# developers expect to go looking for), whereas an added
constructor overload is a completely standard, discoverable ASP.NET MVC
pattern. Chosen deliberately at the cost of touching three controller
files instead of zero.

**Verified:** full suite (`Category!=Integration`, 80 tests) passes via
CLI `vstest.console.exe`, and `src/NerdDinner.csproj` rebuilds clean.
Confirmed inside Visual Studio's Test Explorer — see DL-026's closing
note.

**Status:** Adopted, confirmed inside Visual Studio's Test Explorer.

---

### DL-026 — Same class of bug, third mechanism: `GeolocationService`'s static methods read `ConfigurationManager.AppSettings` directly, with no injectable seam at all

**Decision:** `GeolocationService.PlaceOrZipToLatLong` and
`.HostIpToPlaceName` (both `static`, `src/Services/GeolocationService.cs`)
each gained an additional optional parameter
(`geoNamesUserName`/`ipInfoDbKey`, both defaulting to `null`) that, when
supplied, is used directly instead of the
`ConfigurationManager.AppSettings[...]` lookup. Default behavior
(parameter omitted) is unchanged — still the same
`ConfigurationManager`-based lookup as before, which is all
`SearchController` (the only production caller) ever uses. A new
`NerdDinner.Tests/TestSupport/TestAppSettings.cs` (same shape as
DL-024's `TestConnectionStrings`, `Assembly.CodeBase`-derived path +
`ConfigurationManager.OpenMappedExeConfiguration`) resolves appSettings
values from this assembly's own compiled config directly. All three
`GeolocationServiceTests` now pass their value through explicitly.

**Context:** With DL-023/024/025 all confirmed fixed, the user's next VS
Test Explorer run reduced to exactly the two GeoNames Integration tests
failing — `Uri.EscapeDataString(null)` inside `PlaceOrZipToLatLong`.
Initially indistinguishable from "the GeoNames username secret just
isn't set on this machine" (DL-013's already-documented failure mode for
that case) — but the user had it set, and *had* previously seen these
tests pass. Debugging confirmed the real cause directly: breaking on
`ConfigurationManager.AppSettings` inside the running test showed its
only key was `TestProjectRetargetTo35Allowed` — a legacy VS/MSTest test
host config key, not anything from `NerdDinner.Tests`' own `App.config`.
The exact same AppDomain-config mismatch as DL-023/024/025, hit through
a third mechanism: a `static` service method with no constructor to add
an overload to, unlike the controllers DL-025 fixed.

**A real risk checked empirically before trusting it, not assumed:**
`GeoNames:UserName`'s actual value comes from a config builder
(`Microsoft.Configuration.ConfigurationBuilders.UserSecrets`, per
DL-013) reading `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json` at
runtime — the value checked into `App.config` itself is a blank
placeholder. Whether `ConfigurationManager.OpenMappedExeConfiguration`
(a separate, standalone configuration-loading path from the AppDomain's
ambient `ConfigurationManager.AppSettings`) still applies that config
builder transformation, or just returns the literal blank from the file,
was a real open question — config builders operating on the ambient
config system doesn't guarantee they also run for an explicitly
memory-mapped configuration object. Verified directly rather than
assumed: `PlaceOrZipToLatLong_ReturnsCoordinates_ForKnownValidZip`
(which asserts `Assert.NotNull(result)` against a real GeoNames API
call) passed via CLI `vstest.console.exe` after this change — a blank
username would 401 and produce `null`, so this specifically proves the
real, config-builder-supplied secret was resolved correctly, not just
that the code compiled.

**Verified:** full suite, unfiltered (83 tests, `Category=Integration`
included) passes via CLI `vstest.console.exe`; `src/NerdDinner.csproj`
rebuilds clean.

**Status:** Adopted, confirmed inside Visual Studio's Test Explorer —
the user ran the complete suite there directly and confirmed everything
passes. This closes the DL-023 through DL-026 chain: what began as
"29 failing tests, spread across everything" in VS Test Explorer, root
caused to a single underlying issue (VS's IDE-hosted Test Explorer
AppDomain not resolving this test assembly's own directory/config the
way `AppDomain.CurrentDomain.BaseDirectory`/`ConfigurationManager`
implicitly assume), surfacing through four different mechanisms
(native DLL loading, fixture-owned `DbContext`s, controller-owned
`DbContext`s, and a static service's `AppSettings` read) as each prior
fix peeled back the one before it.

---

### DL-027 — `GeolocationService`'s DL-026 optional-parameter fix is a deliberate stopgap; proper DI belongs to M9

**Decision:** `GeolocationService.PlaceOrZipToLatLong`/`.HostIpToPlaceName`'s
optional-parameter fix (DL-026) stays as-is for the remainder of Phase 1.
Not converting `GeolocationService` to an instance class with real
constructor-injected configuration now, even though that would be the
cleaner design and the user raised it as a live option.

**Reasoning:** `SearchController` (the only caller of these methods) is
already in M9's scope for a full port to the ASP.NET Core app. Real
dependency injection (`IOptions<T>`, constructor injection via the
built-in container) is free and idiomatic there; retrofitting an
instance/DI shape onto a static-method class in classic ASP.NET MVC now
would likely be redone from scratch during that port anyway. The
controllers' constructor injection (DL-025) was different in kind — the
minimum change needed to make permanent test infrastructure work, not a
design upgrade — whereas this would be a design upgrade to code already
scheduled for a rewrite.

**Status:** Adopted. Revisit at M9: give `GeolocationService`'s ASP.NET
Core replacement real constructor-injected configuration from the start,
rather than porting forward the optional-parameter pattern.

---

### DL-028 — M9: Dinners, RSVP, and Search ported to `NerdDinner.Proxy`; EF Core + NetTopologySuite replaces EF6 + DbGeography

**Decision:** `DinnersController`, `RSVPController`, and `SearchController`
are now implemented for real inside `src-core/NerdDinner.Proxy`, backed
by a new EF Core `NerdDinnerCoreContext` pointed at the **same physical
LocalDB database** (`NerdDinner`) the legacy app's EF6 `NerdDinnerContext`
already uses — this is a strangler-fig cutover, not a data migration, so
the existing schema (from `src/Migrations`) is reused as-is. `Dinner.Location`
is `NetTopologySuite.Geometries.Point?` (nullable, matching the legacy
schema's nullable `geography` column) instead of `DbGeography`, mapped
via `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite`.
`Microsoft.EntityFrameworkCore.Proxies` + `UseLazyLoadingProxies()`
restores EF6's default lazy-loading-on-`virtual`-navigation behavior, so
action bodies port over close to verbatim (`db.Dinners.Find(id)` then
touching `dinner.RSVPs` directly) rather than needing every call site
rewritten to explicit `.Include(...)`.

**Verified against the real shared database, not just reasoned about:**
before writing any controller, a throwaway diagnostic endpoint confirmed
EF Core + NTS correctly reads the existing seeded dinners (Seattle/
Portland, SRID 4326) through the exact `geography` column the legacy
EF6 Migrations created — then removed once confirmed. Later, application
logs from a live run showed the real generated SQL, including
`WHERE [d].[Location].STDistance(@sourcePoint) < 2000.0E0` — confirming
the spatial `.Distance()` LINQ call genuinely translates to SQL Server's
native geography operator, not merely that a query returns a plausible
answer.

**Search's route shape had to change internally, not externally:**
classic ASP.NET Web API's action selector let `SearchByLocation`
(GET, `latitude`/`longitude`) and `SearchByPlaceNameOrZip` (GET,
`location`) coexist on the identical `api/Search` route, disambiguated
by which query-string parameters were present. ASP.NET Core's router
doesn't replicate that. Folded into one `[HttpGet] Get(...)` action that
does the same dispatch explicitly. `NerdDinner.js`'s calls
(`GET api/Search?location=...`, `POST api/Search?limit=...`) are
byte-for-byte unchanged, so no client-side changes were needed —
confirmed live via the proxy, not assumed.

**JSON casing preserved deliberately:** the legacy `SearchController`
never configured a camelCase contract resolver, so its Web API JSON
formatter emitted PascalCase property names as declared
(`Title`, `Url`, `RSVPCount`, etc.) — confirmed by checking
`WebApiConfig.cs` directly rather than assuming ASP.NET Core's own
default (camelCase) was safe to keep. `NerdDinner.js` (carried over
unchanged since M8) binds to those exact PascalCase names, so
`AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null)`
was necessary, not cosmetic.

**A real bug found by an actual failed insert, not by reading the code:**
the first version of `Dinner.Location` was declared as a non-nullable
`Point` (matching the legacy C# property's apparent shape, which had no
`?` either since `DbGeography` is itself a reference type with no
compile-time nullability tracking under EF6). With
`<Nullable>enable</Nullable>` on this project, EF Core's convention-based
model building treats a non-nullable reference-type property as a
*required* column — diverging from the legacy schema, where the
`geography` column is nullable (no `nullable: false` in the original
migration) and the model layer explicitly permits a dinner with no
location (`SearchController.JsonDinnerFromDinner`'s characterized
NRE-on-null-`Location` bug depends on exactly this being legal). Caught
when `CreatePost_AddsDinnerWithHostAsFirstRSVP` failed with
`Cannot insert the value NULL into column 'Location'` against the test
database — fixed by declaring `Point? Location` instead.

**Auth-gated actions (`Create`/`Edit`/`Delete`/RSVP `Register`) are a
known, visible interim gap, same pattern as M8/DL-022:** this app
doesn't share the legacy app's OWIN authentication cookie yet (that's
explicitly M10's job). `[Authorize]` needs *some* configured scheme to
challenge against or ASP.NET Core throws rather than redirecting — a
minimal cookie scheme is registered so it redirects cleanly to
`/Account/Login`, which still correctly falls through the YARP proxy to
the legacy app's real login page (confirmed live), even though logging
in there won't leave this app recognizing the user. Honest and visible,
not silently broken.

**`GeolocationService` ported with real constructor-injected
configuration from the start** (`IConfiguration`/`IMemoryCache` via
ASP.NET Core's built-in DI), per DL-027's explicit plan to do this
properly here rather than carry forward the legacy app's
`ConfigurationManager`-coupled, optional-parameter stopgap (DL-026).
This also made something genuinely untestable in the legacy suite
testable here: `SearchByLocation`'s spatial query needs no live network
call (only `SearchByPlaceNameOrZip`'s geocoding path does), so — unlike
`NerdDinner.Tests.Controllers.SearchControllerTests`, which documents
this as an untestable gap — the new suite has real, network-free
coverage of the distance query and the coordinate round-trip.

**Pre-existing characterized bugs ported as-is, not fixed** (DL-004):
`DinnersController.DeleteConfirmed` and `RSVPController.RegisterForDinner`
(via `Register`) still throw unhandled `NullReferenceException` for a
nonexistent id, same as the legacy app — both re-characterized directly
against the new controllers.

**Testing, three layers:**
1. `NerdDinner.Proxy.Tests/Controllers/*` — direct controller
   instantiation + a fake-`ClaimsPrincipal` `SetFakeUser` helper (same
   philosophy as the legacy suite's `ControllerTestHelpers`), against a
   dedicated `NerdDinnerProxyTests` LocalDB catalog (separate from both
   the legacy suite's `NerdDinnerTests` and the shared dev `NerdDinner`
   database) created/dropped per run via `EnsureDeleted`/`EnsureCreated`.
2. `HomeRoutingTests` extended to confirm `Dinners`/`Search` are now
   genuinely served by the new app (not proxied) through the real
   routing path, and its `UnmigratedRoute_IsForwardedToTheLegacyApp`
   test moved from `/Dinners` (now migrated) to `/Account/Login` (still
   genuinely unmigrated) — a deliberate update to reflect real changed
   behavior, per DL-004, not a silent patch to keep it passing.
3. New `ViewRenderingTests`, using a fake `TestAuthHandler` authentication
   scheme (a standard ASP.NET Core testing pattern) through a real
   `WebApplicationFactory` HTTP request — confirms the `[Authorize]`-gated
   `Create`/`Edit` views actually render through the real MVC view engine
   and `EditorTemplates` (`Point`, `LocationDetail`, `CountryDropDown`)
   for an authenticated user, a gap the controller-level tests can't
   cover since they never invoke the view engine.

All three layers pass: 33/33 in `NerdDinner.Proxy.Tests`
(`Category=Integration` tests run against the live legacy app, same
convention as M8); the legacy `NerdDinner.Tests` suite is untouched by
this milestone (no `src/` files modified) and remains at 83/83 per
DL-023 through DL-026.

**Confirmed end to end by the user, beyond what this session could
verify itself:** a `NerdDinner.Proxy`-specific `secrets.json` (own
`UserSecretsId`, separate from the legacy app's per DL-013) was
configured locally with a real GeoNames username, confirming
`SearchByPlaceNameOrZip`'s live geocoding path — untested by this
session since the automated suite deliberately doesn't depend on a
locally-configured secret, same convention as the legacy app's
GeoNames integration tests. Separately, a dinner created without a
geocoded `Location` (no Bing Maps key configured locally) reproduced
the characterized `JsonDinnerFromDinner` NRE live against real data —
removed directly from the shared dev database (`DELETE` by `DinnerID`,
confirmed via `sqlcmd` before and after) rather than through the app,
since the bug being characterized is exactly "no way to clean this up
through the UI." The `[Authorize]`-gated `Dinners/Create` ("Host
Dinner") redirect-loop-to-legacy-login behavior was confirmed live and
matched this entry's documented M10 gap exactly, with no surprises.

**Status:** Adopted.

---

### DL-029 — M10: Account migrated to `NerdDinner.Proxy`; ASP.NET Core Identity replaces ASP.NET Identity 2.x/OWIN

**Decision:** `AccountController` and all its views/partials are now
implemented for real inside `src-core/NerdDinner.Proxy`, backed by
ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`)
in place of ASP.NET Identity 2.x + OWIN. `ApplicationUser`/`ApplicationDbContext`
mirror the legacy shape; the legacy app's `[ChildActionOnly]` actions
(`ExternalLoginsList`, `RemoveExternalLogins`) became View Components
(`ExternalLoginsListViewComponent`, `RemoveExternalLoginsViewComponent`)
— ASP.NET Core MVC's replacement for classic MVC's child actions.
`AccountController.ChallengeResult` (a hand-rolled `ActionResult`
wrapping OWIN's `Authentication.Challenge`) is gone; ASP.NET Core MVC
ships `ControllerBase.Challenge(...)` natively. The four external OAuth
providers (Google, Microsoft, Twitter, Facebook) are wired up with the
same conditional-on-non-empty-secret pattern as the legacy
`Startup.Auth.cs` — none are configured in this dev environment (all
ship blank in `appsettings.json`, matching the legacy `Web.config`),
so these flows are structurally ported but not live-tested, same
documented limitation the legacy suite already had.

**A real, live-discovered schema incompatibility, not caught by
reasoning:** the shared `NerdDinner.Identity` LocalDB database (which
`NerdDinnerCoreContext`'s M9 precedent suggested reusing as-is) was
created by the legacy app's ASP.NET Identity 2.x/EF6 schema
(`LockoutEndDateUtc` as `DateTime`, no `NormalizedUserName`/
`NormalizedEmail`/`ConcurrencyStamp`). ASP.NET Core Identity's own
schema is a real superset with different columns — unlike M9's Dinners
table, these two are not wire-compatible. Found by an actual failed
`Register` POST (`SqlException: Invalid column name 'NormalizedUserName'`),
not by inspecting the schema in advance. The existing database had
exactly one user row — consistent with DL-014's identical M4 finding
("no real user data in this practice project"), so rather than
hand-migrate a schema for data not worth preserving, `NerdDinner.Identity`
was dropped (`DROP DATABASE`, confirmed with the user first given its
destructive nature) and `ApplicationDbContext.Database.EnsureCreated()`
now builds the correct ASP.NET Core Identity schema fresh at startup —
same "no formal migrations, no seed data needed" reasoning DL-018
already used for `DefaultConnection` in the legacy app.

**A second real bug, also only found by an actual failed POST:**
`Dinner.RSVPs` (declared as non-nullable `ICollection<RSVP>`,
deliberately not default-initialized since M9/DL-028, to preserve the
characterized `IsUserRegistered` NRE-on-null behavior) made every
`Dinners/Create` and `Dinners/Edit` POST fail ModelState validation
with "The RSVPs field is required" — under `<Nullable>enable</Nullable>`,
ASP.NET Core MVC's automatic model validation treats a non-nullable
reference-type property as implicitly required, even though no view
ever posts an "RSVPs" field (it's set server-side in the controller).
This surfaced only once M10 made it possible to actually complete an
authenticated POST for the first time — M9's own `DinnersControllerTests`
never caught it because those tests call the controller action directly
(bypassing MVC's model-binding/validation pipeline entirely), and the
one live `curl` verification M9 did (DL-028) never exercised a
successful Create POST specifically, only GETs and Search's read paths.
Fixed with `[BindNever]` (stop MVC from trying to populate `RSVPs` from
posted data) *and* `[ValidateNever]` (stop MVC from separately flagging
the still-null result as a validation error) — `[BindNever]` alone was
tried first and confirmed insufficient by re-triggering the exact same
failure, not assumed to be enough.

**Verified live, the actual full loop, not just unit-level:** registered
a real user via HTTP POST to `/Account/Register`, confirmed `_LoginPartial`
correctly showed the authenticated state (finally resolving the
M8-era/DL-022 always-logged-out placeholder for real), confirmed
`/Dinners/Create` — a completely different, `[Authorize]`-gated
controller — recognized the same session and rendered the real form
(not a redirect to legacy's login page), submitted a real dinner and
confirmed it landed in the shared dev database, confirmed `Details`
correctly showed "You are the host," logged off, and confirmed
`/Dinners/Create` correctly redirected to login again afterward. Test
data cleaned up from the shared dev database after verification.

**Testing:**
- `AccountControllerIdentityTests` (ported from the legacy suite):
  registration success/duplicate-rejection/short-password-rejection,
  password check success/failure, against a dedicated
  `NerdDinnerProxyIdentityTests` LocalDB catalog (own
  `ProxyIdentityTestDatabaseFixture`, separate from both the shared dev
  `NerdDinner.Identity` and `NerdDinnerCoreContext`'s test database) —
  built via a real DI container running the exact same `AddIdentity(...)`
  configuration `Program.cs` uses, rather than hand-constructing
  `UserManager` (whose real constructor has several required
  collaborators easy to get subtly wrong by hand).
- New `AuthFlowTests` (`Category=Integration`): real end-to-end HTTP
  through `WebApplicationFactory`'s cookie-aware default client (no fake
  auth scheme) — `RegisteredUser_SessionIsRecognized_ByADifferentAlreadyMigratedController`
  is the direct, permanent test of M10's "session handling... explicitly
  tested, not assumed" acceptance criterion;
  `EditView_RendersForRealOwner_AfterRealCreateAndOwnershipCheck`
  supersedes M9's fake-auth-scheme `ViewRenderingTests` (retired here —
  it doesn't compose cleanly with ASP.NET Core Identity's own scheme
  setup, confirmed by an actual failing run, not assumed);
  `UnauthenticatedRequest_ToProtectedRoute_RedirectsToLogin` confirms the
  negative case.
- `HomeRoutingTests.UnmigratedRoute_IsForwardedToTheLegacyApp` (M8),
  moved to `/Account/Login` (M9), is retired again -- renamed
  `GlimpseAxd_IsStillForwardedToTheLegacyApp` and pointed at
  `glimpse.axd`, since every legacy *feature* route is now migrated and
  `glimpse.axd` is genuinely legacy-only, staying that way until M11
  removes it entirely (DL-019) — the first target for this test that
  won't need chasing again next milestone.

All new/updated tests pass: 40/40 in `NerdDinner.Proxy.Tests`. The
legacy `NerdDinner.Tests` suite is untouched by this milestone (no
`src/` files modified) and remains at 83/83 per DL-023 through DL-026.

**Status:** Adopted.

---

### DL-030 — Documented gap: Bing Maps' free tier is retired; a modern replacement (Azure Maps or similar) was never surfaced by the original assessment or M1

**Decision:** Not fixed now. Logged as a known, deliberately deferred
gap — the app already tolerates it gracefully (a `Dinner` with no
`Location` is legal by the model's own contract, no `[Required]`
attribute; see DL-015/DL-028's characterized behavior for exactly this
case). No milestone in `plan.md` (M1–M11) currently owns replacing the
mapping provider.

**Context:** Found live, not by inspection: creating a dinner through
`NerdDinner.Proxy`'s Create form saves it with `Location = NULL`
because the client-side Bing Maps control (`NerdDinner.js`'s
`Microsoft.Maps` API calls: `LoadMap`, `LoadPin`, `GetCenter`,
`SetZoomLevel`, wired up via `_Layout.cshtml`'s
`mapcontrol.ashx`-sourced `<script>` tag) never successfully geocodes
an address without a working Bing Maps key — and free Bing Maps keys
are no longer obtainable at all; Microsoft retired that tier in favor
of Azure Maps. `SearchController.JsonDinnerFromDinner`'s already-
characterized NRE-on-null-`Location` bug (DL-028) is what actually
surfaces this to a user: visiting the front page after creating an
un-geocoded dinner throws, because `NerdDinner.FindMostPopularDinners`
calls `POST api/Search` unconditionally.

**Why this wasn't caught earlier, worth recording as a real process
gap:** M1's dependency research (`m1-dependency-research.md`) audited
`packages.config` — NuGet package dependencies — and correctly caught
other stale/retired dependencies that way (DotNetOpenAuth, jQuery,
X.PagedList, per DL-009/DL-014). The Bing Maps AJAX Control was never a
NuGet package; it's a directly-referenced external script tag and
hosted service, invisible to a packages-manifest audit. Grepped both
`assessment.md` and `m1-dependency-research.md` directly to confirm
neither mentions Bing Maps at all, rather than assuming the gap.
**Lesson for future assessments of this kind:** dependency research
needs to explicitly include externally-loaded scripts/services
referenced directly in views/layouts, not just what's declared in a
package manifest — a category of dependency this engagement's process
didn't have a checklist item for.

**Replacement direction, if/when this gets scoped as real work:** Azure
Maps was identified as the natural target (same Microsoft ecosystem as
the rest of this modernization) over Google Maps. Real scope, not a
config swap: `NerdDinner.js`'s `Microsoft.Maps`-API calls would need a
genuine rewrite against Azure Maps' JS SDK (`atlas.Map`, a different API
shape), touching every view that renders a map (`Home/Index`,
`Dinners/Create`, `Dinners/Edit`, the `LocationDetail` templates).

**Confirmed by the user's own independent manual verification after M11:**
the completed, renamed `NerdDinner` app was exercised by hand end to
end and found working correctly in every area except this one, already-
documented gap — no new issues surfaced. The scope of what M11 verified
live (this session's own testing) and what the user verified
independently now agree.

**Status:** Adopted (as a documented, deferred gap — not a fix).

---

### DL-031 — M11: legacy Framework app and reverse proxy decommissioned; `NerdDinner.Proxy` renamed to `NerdDinner`

**Decision:** The legacy .NET Framework application (`src/`, the
original MVC4/.NET 4.5 codebase this whole engagement started from,
upgraded through Phase 1) and `NerdDinner.Tests` (its characterization
suite) are removed from the working tree via `git rm` — recoverable
from git history (the `bf314f5` baseline import and every commit since),
not gone forever, same no-history-rewrite approach DL-018 used for the
checked-in `.mdf`/`.ldf` files at M6. The now-orphaned `packages/`
folder (NuGet packages for the deleted classic-framework projects,
already gitignored) is deleted outright.

`src-core/NerdDinner.Proxy` — the ASP.NET Core app built up across
M7–M10 — is renamed to `NerdDinner` and moved to `src/NerdDinner`,
replacing the deleted legacy app at the path a reader would naturally
expect to find "the app." Its test project moves from
`src-core/NerdDinner.Proxy.Tests` to top-level `NerdDinner.Tests`,
reoccupying the exact name and location the legacy test project used.
Every `NerdDinner.Proxy.*` namespace collapses to `NerdDinner.*`
throughout (`NerdDinner.Proxy.Controllers` → `NerdDinner.Controllers`,
etc.) via a single substring find/replace (`NerdDinner.Proxy` →
`NerdDinner`, safe here because every variant shares that literal
prefix). Test infrastructure classes that had picked up a "Proxy"
qualifier along the way during the transition
(`ProxyTestDatabaseFixture`, `ProxyIdentityTestDatabaseFixture`, their
`Proxy*.cs` filenames, and their LocalDB catalog names
`NerdDinnerProxyTests`/`NerdDinnerProxyIdentityTests`) are renamed too,
back to what the original legacy suite called its equivalents
(`TestDatabaseFixture`, `IdentityTestDatabaseFixture`,
`NerdDinnerTests`/`NerdDinnerIdentityTests`) — no collision risk now
that the legacy suite that first used those names is gone.

**Reasoning for the rename (a live choice, not a default):** the user
was asked directly whether to rename now that the project is no longer
a proxy in front of anything, versus leaving the M7-era name as a
historical artifact. Chosen to rename despite the larger mechanical
footprint (namespace-wide find/replace, `.csproj`/`.sln` updates, both
test projects' references) because the end state is meant to be the
final, permanent shape of this codebase, not a transitional one — a
project called "Proxy" that proxies nothing would be a confusing
artifact for anyone approaching the finished repository cold.

**Proxy infrastructure removed from `Program.cs`/`appsettings.json`:**
`AddReverseProxy()`/`.LoadFromConfig(...)`, `MapReverseProxy()`, the
`ReverseProxy` config section, the `Yarp.ReverseProxy` package
reference, and the `/_proxy/health` diagnostic endpoint (M7's own
verification tool, no longer meaningful with nothing to route between)
are all gone. The default MVC route's `constraints: new { controller =
"Home|Dinners|RSVP|Account" }` — needed at M8–M10 so unmigrated
controllers would fall through to the YARP catch-all — reverts to the
plain, unconstrained default route, since there's no fallback left to
defer to.

**Verified live, the actual final shape, not just "it compiles":**
started the renamed `NerdDinner` app alone (no legacy app, no IIS
Express, nothing else running) and confirmed Home, Dinners,
Account/Login, and `api/Search` all serve correctly; confirmed the
removed `/_proxy/health` and `glimpse.axd` (both real, live-forwarded
endpoints as recently as M10) now correctly 404 with nothing to forward
to; ran a full register → authenticated `Dinners/Create` → real dinner
save cycle standalone, confirming M10's auth work holds with zero
proxy/legacy dependency anywhere in the loop.

**Test suite updated to match, not just made to compile:**
`HomeRoutingTests` (originally built at M8 specifically to prove the
proxy's routing decision) is retired to plain smoke tests — there's no
more "served by the new app" vs. "forwarded to legacy" distinction to
prove, so its `_NotProxied` framing and its `GlimpseAxd_...`/
`ProxyHealthEndpoint_...` tests (both asserting on infrastructure that
no longer exists) are removed; a new
`NonexistentRoute_Returns404_NotSwallowedByAFallback` test replaces them,
confirming an unmatched route now genuinely 404s rather than silently
vanishing into a catch-all. `AuthFlowTests` keeps its
`Category=Integration` tag, but for a different, now-accurate reason:
not a live external dependency (that reason — the legacy app running —
is gone), but because it's the one test class that writes to the shared
dev `NerdDinner` database, kept out of the default fast run so ordinary
cycles don't depend on or perturb it.

**A real, unrelated bug resurfaced during this milestone's test run,
not caused by it:** `SearchApi_Serves` failed against the live shared
dev database because a dinner created earlier (`DinnerID 1005`, "My
Dinner," `Location = NULL` — the same characterized
`JsonDinnerFromDinner` NRE from DL-028, root-caused live to a missing
Bing Maps key and logged as a deferred gap in DL-030) was still present.
Removed directly from the database (confirmed with the user first,
given the destructive nature of the action) rather than treated as an
M11 regression — re-ran the full suite clean afterward.

**Final verification:** 39/39 passing in the renamed `NerdDinner.Tests`
against `NerdDinner`, the single, sole application. No `src/`
(legacy) files exist anymore for any characterization test to target —
this milestone's own test suite *is* "the final characterization
suite" plan.md's M11 acceptance criteria asks to pass.

**Status:** Adopted.

---

### DL-032 — `JsonDinnerFromDinner`'s NRE-on-null-`Location` characterized bug fixed for real: falls back to (0, 0)

**Decision:** `SearchController.JsonDinnerFromDinner` no longer throws a
`NullReferenceException` for a `Dinner` with no geocoded `Location`. It
now uses `(0, 0)` as a placeholder for `Latitude`/`Longitude`
(`dinner.Location?.Y ?? 0`, `dinner.Location?.X ?? 0`) and returns
normally. This is a deliberate, explicit fix requested by the user —
not a silent one, and not something this session decided unilaterally.

**Context:** This was a real, pre-existing bug in the original 2012
baseline, deliberately characterized rather than fixed at M2 (per
DL-004) and ported forward unchanged through M9 (DL-028) as "capture
current behavior, bugs included." That discipline made sense while
Bing Maps worked and a null `Location` was a genuine edge case. Once
DL-030 established that Bing Maps' free tier is retired with no in-plan
replacement, a null `Location` stopped being an edge case — it's now
the default outcome of every dinner creation, since the client-side map
control that would populate it never successfully geocodes anything.
This session hit the resulting crash live, repeatedly, across several
different leftover dinners (DL-031's "My Dinner"/`DinnerID 1005`, and
this session's "Justin's Dinner"/`DinnerID 1010`) — each time requiring
a manual `DELETE` from the database to unblock the front page, which
depends on `GetMostPopularDinners` succeeding. The user asked directly
to stop treating this as a characterized bug and fix it.

**Reasoning for `(0, 0)` specifically, not filtering the dinner out or
some other placeholder:** matches exactly what the user asked for.
`(0, 0)` is a real, if nonsensical, coordinate (the "Null Island" point
in the Gulf of Guinea) — chosen here purely as an inert placeholder
value the JSON contract already has a slot for (`Latitude`/`Longitude`
are non-nullable `double` on `JsonDinner`), not as a claim that it's
meaningful. `NerdDinner.js`'s client-side rendering doesn't currently
plot popular-dinner markers using this endpoint's coordinates in a way
that would visibly place a pin at the wrong spot on a map users can
see — the practical effect is just that the dinner appears in the
"Popular Dinners" list without crashing anything, which is what the
user's fix is for.

**Test updated deliberately, per DL-004's own standard for exactly this
situation** ("if a change legitimately changes observable behavior,
that's a decision-log entry, and the test gets updated deliberately
with a comment explaining the change — never silently"):
`JsonDinnerFromDinner_ThrowsNullReferenceException_WhenDinnerHasNoLocation`
is renamed `JsonDinnerFromDinner_UsesZeroZeroCoordinates_WhenDinnerHasNoLocation`
and now asserts `Latitude == 0 && Longitude == 0` for a dinner with a
null `Location`, instead of asserting the old NRE.

**Verified live**, not just via the updated unit test: inserted a real
null-`Location` dinner directly into the shared dev database, hit
`POST api/Search?limit=10` (the exact endpoint `NerdDinner.js`'s
`FindMostPopularDinners` calls from the front page), and confirmed a
`200` response with that dinner correctly present at
`"Latitude":0,"Longitude":0` rather than the request failing entirely.
Verification dinner removed afterward.

**Status:** Adopted.
