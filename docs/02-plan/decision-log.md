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
