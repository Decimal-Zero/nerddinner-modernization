# M2 — Characterization Test Suite

Establishes the safety net against the application's **original,
unmodified baseline behavior** — before M3's framework upgrade touches
anything. See `decision-log.md` DL-004 for why this ordering matters.

**Status: verified.** Built and run against LocalDB. All tests pass,
including the two GeoNames tests that were skipped at first — see
"Known issue found during verification" below for how that got
resolved (during M3, not M2 itself). Two real problems surfaced during
the build process itself before that point:

1. An invalid XML comment (a double-dash inside `<!-- -->`) in
   `NerdDinner.Tests.csproj` failed the project load entirely. Fixed;
   every `.csproj`/`.config` file in the repo was then checked for the
   same pattern and confirmed clean.
2. `ws.geonames.org`, hardcoded in `GeolocationService`, has been
   retired — see below.

Both are good evidence for the case study's larger point: authoring
tests carefully isn't the same as verifying them, and assessment-by-reading
doesn't catch everything assessment-by-running does.

## Known issue found during verification: `ws.geonames.org` retired (resolved during M3)

`GeolocationService.PlaceOrZipToLatLong` originally hardcoded
`http://ws.geonames.org/postalCodeSearch?...`. That subdomain is no
longer in service; the replacement is `api.geonames.org`. The two tests
exercising this path (`PlaceOrZipToLatLong_ReturnsCoordinates_ForKnownValidZip`,
`PlaceOrZipToLatLong_ReturnsNull_WhenNoResultsFound`) were marked
`[Fact(Skip = "...")]` at the time this note was first written, rather
than deleted or left failing.

This was worth calling out beyond "one more bug list item": the original
assessment's Category 7 finding was, verbatim, that `GeolocationService`
has *"no fallback, retry, or circuit breaker if either free third-party
geocoding API is unavailable or rate-limits."* That finding was based on
reading the code. This is the same finding, confirmed by the dependency
actually going away during the course of this engagement — a concrete
demonstration of why "no fallback" was flagged as a real risk rather than
a theoretical one.

**Resolution:** the endpoint swap turned out to be exactly the drop-in
replacement hoped for — `api.geonames.org`'s response format matches
`ws.geonames.org`'s closely enough that the existing
`Descendants("code")`/`Element("lat")`/`Element("lng")` parsing needed no
changes. That swap is reflected in `plan.md` M5's acceptance criteria as
already satisfied, ahead of the rest of that milestone. A second,
unrelated issue surfaced once the endpoint actually responded instead of
failing to resolve: `api.geonames.org` requires a registered username on
every request, a policy GeoNames added after this engagement's M1
dependency research was written. That's wired up via a local
user-secrets store, not checked into the repo — see `decision-log.md`
DL-013 for the mechanism. Both tests now run unskipped and pass against
the live API (still tagged `Category=Integration`, so still excluded
from the default fast run — set your own GeoNames username locally per
DL-013 before running them).

## Sandbox limitation and how verification actually happened

This sandbox has no .NET SDK, no Mono, and no NuGet registry access
(`nuget.org` isn't reachable from this environment). Every test in
`NerdDinner.Tests` was written carefully against known-correct
xUnit/Moq/EF6 patterns and against the actual source of the controllers
and models being tested — but none of it was compiled or run here.
Verification happened separately, in Visual Studio against LocalDB, and
came back green (with the two documented `Skip` exceptions above) —
confirming the tests were sound, but also surfacing the two real issues
recorded at the top of this document that only showed up once something
actually tried to build and run the code.

### How it was run

1. Open `NerdDinner.sln` in Visual Studio (2017+; the solution already
   references `NerdDinner.Tests.csproj`, added by this commit).
2. Right-click the solution → **Restore NuGet Packages** (pulls xUnit
   2.4.2, Moq 4.18.4, and their dependencies per `packages.config`).
3. Build. LocalDB (installed with Visual Studio by default) is required
   for the DB-backed test collection — no separate setup needed beyond
   having it installed.
4. Test Explorer → Run All. To skip the network-dependent integration
   tests during normal runs:
   ```
   vstest.console.exe NerdDinner.Tests\bin\Debug\NerdDinner.Tests.dll /TestCaseFilter:"Category!=Integration"
   ```

## Coverage

| Area | Coverage | Approach |
|---|---|---|
| `Dinner` model (validation, `IsHostedBy`, `IsUserRegistered`, `LocationDetail`) | Full | Pure unit tests, no infrastructure |
| `AccountModels` (Login/Register/LocalPassword validation) | Full | Pure unit tests |
| `StringExtensions` (`Truncate`, `IsNumeric`) | Full | Pure unit tests |
| `DinnersController` | Ownership checks, paging, filtering, Create prefill | Direct controller instantiation + Moq'd `ControllerContext`, LocalDB-backed |
| `RSVPController` | Registration idempotency, cancellation | Same pattern |
| `HomeController` | Full (trivial) | Same pattern |
| `SearchController` | Non-network-dependent branches, popularity ordering | Same pattern |
| `GeolocationService` | Current behavior against the real APIs, including failure paths | Integration-tagged, excluded from default runs |
| `AccountController` | One side-effect-free action (`ExternalLoginFailure`) | Direct instantiation |

## Real bugs found and characterized, not fixed

Per DL-004, this suite documents current behavior — including bad
behavior — rather than quietly correcting it. Three found while writing
these tests:

- **`DinnersController.DeleteConfirmed`** throws an unhandled
  `NullReferenceException` for a nonexistent dinner id, instead of the
  `HttpNotFound` the GET `Delete` action returns for the same case.
- **`RSVPController.Register`** has the identical gap — no null check
  after `db.Dinners.Find(id)` before calling `IsUserRegistered`.
- **`SearchController.JsonDinnerFromDinner`** unconditionally
  dereferences `dinner.Location.Latitude`/`.Longitude`. A dinner with no
  `Location` set (which the model permits — `Location` has no
  `[Required]` attribute) throws `NullReferenceException` the moment
  it's serialized, rather than being filtered out or handled gracefully.

None of these were mentioned in the original assessment (`docs/01-assessment/assessment.md`)
— they surfaced only from actually exercising the code while writing
characterization tests, which is itself worth noting for the case study:
assessment-by-reading and verification-by-testing find different things.

## Documented gaps (not oversights — deliberate scope decisions)

- **`AccountController`'s core flows** (Login, Register, external OAuth)
  are built on SimpleMembership/DotNetOpenAuth static state that requires
  a live, initialized ASP.NET runtime to exercise meaningfully. Since M4
  replaces this entire stack, reverse-engineering test seams into code
  scheduled for deletion isn't a good use of effort. The observable
  authentication contract (login redirects, registration succeeds, bad
  credentials show an error) gets characterized at the integration level
  once M4 lands the new auth stack — see `plan.md` M4.
- **`GeolocationService`'s network-dependent paths** can't be unit
  tested without either hitting the live third-party APIs or introducing
  a seam — the service is a static class with no injection point. Handled
  as explicitly-tagged integration tests instead, run deliberately rather
  than on every test pass.
- **`[Authorize]` filter enforcement itself** isn't tested — these tests
  characterize what an action does once invoked, not whether MVC's
  pipeline correctly blocks unauthenticated access before that point.
  That's framework-level behavior, not application logic, and out of
  scope for characterizing *this* codebase's behavior.

## Test data

The DB-backed tests use a dedicated `NerdDinnerTests` LocalDB database
(created, seeded, and dropped automatically per test run — see
`TestSupport/TestDatabase.cs`), deliberately separate from the
`.mdf`/`.ldf` files checked into `src/App_Data`. Reusing those was
considered and rejected: they're exactly the kind of ad hoc, unreproducible
data artifact flagged in the assessment's Category 4 finding, and a test
suite shouldn't depend on the same anti-pattern it exists partly to guard
against.
