# M1 — Dependency Compatibility Research

Full audit of every dependency in `packages.config`, researched against
current (August 2026) status. Each entry gets a disposition: **upgrade
in-place** (Phase 1 target version, same package), **replace now**
(Phase 1, different package), **remove** (not needed going forward), or
**carry to Phase 2** (stays as-is through Phase 1, resolved during the
ASP.NET Core migration instead).

Sources are NuGet package listings and official project documentation,
checked directly rather than assumed from the versions already in the
codebase.

---

## Authentication (resolves DL-006)

| Package | Current | Status | Disposition |
|---|---|---|---|
| `DotNetOpenAuth.*` (6 packages) | 4.3.0 | **Dead.** No release since ~2014; NuGet listing targets .NET Framework 4.5 only, no maintained fork found. Community consensus recommends ASP.NET Identity as the successor. | **Replace now** |
| SimpleMembership (`WebSecurity`) | — | Superseded by ASP.NET Identity in 2014; no further development. | **Replace now** |

**Replacement path:** ASP.NET Identity 2.x + OWIN external-login middleware (`Microsoft.Owin.Security.Google`, `.Facebook`, etc. — currently at 4.2.3, explicitly compatible with .NET Framework 4.8/4.8.1). This is the standard, still-supported migration path for exactly this combination (SimpleMembership + DotNetOpenAuth) and runs natively on .NET Framework via `Microsoft.Owin.Host.SystemWeb` — no need to wait for Phase 2.

**Resolves DL-006:** auth stack replacement happens in Phase 1 (M4), not deferred. Doing it once now, on a still-supported path, is less total work than running unsupported libraries through all of Phase 1 and replacing them twice.

---

## Data access

| Package | Current | Status | Disposition |
|---|---|---|---|
| `EntityFramework` | 5.0.0 | Latest is **6.5.2** (April 2026). EF6 is in Microsoft's Modern Lifecycle Policy — security-fix-only, not actively developed, but explicitly still supported, with no end-of-support date announced. Runs on .NET Framework 4.x. | **Upgrade in-place** to 6.5.2 |
| `Microsoft.SqlServer.Types` | 10.50.1600.1 | Needed for `DbGeography` under EF6. Superseded entirely by NetTopologySuite when EF Core arrives in Phase 2. | **Upgrade in-place** for Phase 1 only; **removed** in Phase 2 |

---

## Web framework

| Package | Current | Status | Disposition |
|---|---|---|---|
| `Microsoft.AspNet.Mvc` | 4.0.20710.0 | Latest is **5.3.0** (Oct 2023, minimal changes from 5.2.9). Maintained via the AspNetWebStack repo; mature/maintenance mode, security patches tied to .NET Framework lifecycle. | **Upgrade in-place** to 5.3.0 |
| `Microsoft.AspNet.Razor` | 2.0.20715.0 | Versioned alongside MVC; current is 3.2.x/3.3.x line matching MVC 5.3.0. | **Upgrade in-place** |
| `Microsoft.AspNet.WebPages*` (4 packages) | 2.0.20710.0 | Same MVC5-line upgrade as above. | **Upgrade in-place** |
| `Microsoft.AspNet.WebApi*` (4 packages) | 4.0.20710.0 | Web API 2.x line, current and compatible with MVC 5.3.0. | **Upgrade in-place** |
| `Microsoft.jQuery.Unobtrusive.Ajax` / `.Validation` | 2.0.30116.0 / 2.0.30116.0 | Tied to the WebPages version above; upgrade together. | **Upgrade in-place** |
| `Microsoft.Net.Http` | 2.0.20710.0 | Superseded by the `System.Net.Http` types built into the framework since 4.5 — this package is a compatibility shim from before that. | **Remove** if nothing depends on the standalone package post-upgrade (verify during M3) |
| `Microsoft.Web.Infrastructure` | 1.0.0.0 | Low-level ASP.NET plumbing dependency; version is effectively fixed by the framework. | **Upgrade in-place** (pulled in automatically by the MVC5 upgrade) |

---

## Diagnostics

| Package | Current | Status | Disposition |
|---|---|---|---|
| `Glimpse`, `Glimpse.AspNet`, `Glimpse.Mvc4` | 1.3.0 / 1.2.1 / 1.2.1 | **Dead.** Last releases were 2013–2014 (`Glimpse.AspNet` 1.9.2, Oct 2014; `Glimpse.Mvc5` 1.5.3, Feb 2014 — no releases since). Microsoft's own tutorial for Glimpse explicitly warns it hasn't been security-audited for production use. | **Remove entirely** — this is a dev-time diagnostics tool that should never have shipped in a deployable config in the first place; not worth replacing, just removing (ties to the Category 5/6 `debug`/`customErrors` findings — same root problem). |

---

## Front-end libraries

| Package | Current | Status | Disposition |
|---|---|---|---|
| `jQuery` | 1.7.1.1 | Latest stable is **4.0.0** (Jan 2026). Only the current major branch gets maintenance. 1.7.1 (2011) has documented XSS CVEs. | **Upgrade in-place** — but flag for regression risk: jQuery 4.0 removes deprecated APIs (`.push`/`.sort`/`.splice` on the jQuery object, old AJAX event aliases) that 2012-era code may use. Characterization tests (M2) should specifically exercise any AJAX/DOM-manipulation-heavy views before and after this bump. |
| `jQuery.UI.Combined` | 1.10.2 | Current jQuery UI is far newer; compatibility with jQuery 4.0 needs verification during M3, not assumed. | **Upgrade in-place**, verify against jQuery 4.0 specifically |
| `jquery.mobile` | 1.3.0 | jQuery Mobile has been discontinued by the jQuery Foundation for years with no successor recommended by the project itself. | **Remove** — the mobile-specific view (`Index.Mobile.cshtml`, `_Layout.Mobile.cshtml`) should be re-evaluated for whether a responsive layout replaces it rather than pulling in an abandoned framework |
| `jQuery.Validation` | 1.11.1 | Actively maintained project (jquery-validation), current releases available. | **Upgrade in-place** |
| `knockoutjs` | 2.2.1 | Latest stable is **3.5.3**. Actively maintained (supports modern Trusted Types/CSP). | **Upgrade in-place** |
| `Modernizr` | 2.6.2 | Feature-detection library for browser capabilities that are now universal — the specific gaps it was built to patch (HTML5 element support, CSS3 features) no longer exist in any currently-supported browser. | **Remove** — carries a maintenance cost with no remaining benefit for this app's supported browser matrix |
| `yepnope.js` | 1.5.4 | The yepnope project was discontinued and folded into Modernizr years ago; no longer maintained standalone. | **Remove** (dependent on Modernizr removal above) |
| `WebGrease` | 1.3.0 | Last released ~2013; bundling/minification engine behind `System.Web.Optimization`. No maintained successor in the same package, but it's the de facto standard for MVC5-era bundling and has no known active CVEs. | **Upgrade in-place if a newer build exists; otherwise carry as-is through Phase 1** — Phase 2's move to ASP.NET Core replaces this mechanism entirely (built-in bundling/modern front-end tooling), so it's not worth replacing twice |

---

## Serialization

| Package | Current | Status | Disposition |
|---|---|---|---|
| `Newtonsoft.Json` | 5.0.4 | Actively maintained (JamesNK/Newtonsoft.Json), regular releases continuing. | **Upgrade in-place** to current 13.x line |

---

## Utilities

| Package | Current | Status | Disposition |
|---|---|---|---|
| `PagedList` | 1.15.0.0 | Original project abandoned by its author. | **Replace now** with `X.PagedList` (actively maintained fork, MIT-licensed, current release 8.x for classic MVC/`System.Web` — the `.Core` line is ASP.NET-Core-only and not relevant until Phase 2) |

---

## Summary by disposition

- **Upgrade in-place (Phase 1):** EntityFramework, Microsoft.SqlServer.Types, all `Microsoft.AspNet.*` packages, jQuery, jQuery UI, jQuery.Validation, knockoutjs, Newtonsoft.Json, WebGrease (if a newer build exists).
- **Replace now (Phase 1):** DotNetOpenAuth + SimpleMembership → ASP.NET Identity + OWIN external auth; PagedList → X.PagedList.
- **Remove entirely:** Glimpse (all 3 packages), Modernizr, yepnope.js, jquery.mobile (pending a decision on whether the mobile view is replaced with responsive layout or dropped).
- **Carry to Phase 2 for final resolution:** Microsoft.SqlServer.Types is retired outright when `DbGeography` is replaced with NetTopologySuite; WebGrease's bundling mechanism is retired outright when ASP.NET Core's own tooling takes over.

## Open item carried forward

Whether the mobile-specific views (`jquery.mobile` dependents) are replaced with a responsive layout during Phase 1 or simply dropped is a scope question, not a technical one — worth a quick decision before M3 starts, logged in `decision-log.md` once made.
