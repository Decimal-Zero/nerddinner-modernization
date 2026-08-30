# NerdDinner Modernization — Decimal Zero Practice Engagement

A practice legacy .NET modernization engagement, run end-to-end using
Decimal Zero LLC's assessment and delivery methodology, against a real
historical codebase: the original **NerdDinner** ASP.NET MVC 4 / .NET
Framework 4.5 sample application.

This is simultaneously:

1. **Reps for the engagement workflow** — assessment, scoping, milestone
   execution, and verified delivery, practiced against a real (if small)
   legacy codebase rather than a synthetic exercise.
2. **A public case study** documenting the choices made and why, intended
   to demonstrate how a modernization engagement is actually run —
   including the assessment methodology and the verification approach
   used to prove correctness, not just describe the end state.

See `NOTICE.md` for where this codebase came from and its license basis
(Microsoft Public License / Ms-PL).

## Status

**Complete.** Both phases of the modernization plan (`docs/02-plan/plan.md`)
have run to completion and been verified:

- **Phase 1** (M1–M6): in-place upgrade to .NET Framework 4.8.x — auth
  stack replaced, security/configuration hardening, dependencies
  brought current, data layer cleaned up.
- **Phase 2** (M7–M11): strangler-fig cutover to ASP.NET Core / .NET 10,
  migrated route-by-route behind a reverse proxy, auth migrated last,
  ending with the legacy .NET Framework application and the proxy both
  fully decommissioned. `NerdDinner` is now a single ASP.NET Core / .NET
  10 application — the legacy codebase this repository started from no
  longer exists in the working tree (recoverable via git history).

See `docs/02-plan/decision-log.md` for the full record of what changed
and why, milestone by milestone.

## Repository structure

```
/src/NerdDinner       Application source (ASP.NET Core / .NET 10)
/NerdDinner.Tests     xUnit test suite
/docs
  01-assessment/    Original modernization assessment scorecard and findings
  02-plan/          Scope, milestones, acceptance criteria, decision log
  03-outcome/       Phase 1 exit checkpoint: before/after verification
LICENSE.txt         Microsoft Public License (Ms-PL)
NOTICE.md           Provenance and license basis
```

## Following along

The commit history is the primary evidence trail — each modernization
step is a small, independently reviewable commit tied to a milestone
in `docs/02-plan/`. The `docs/` directory tells the story; the commits
prove it happened as described.
