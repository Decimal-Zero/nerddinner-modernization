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

**Baseline import.** The legacy application is in place as-received;
assessment has not yet started.

## Repository structure

```
/                   Application source (legacy today; modernized in place
                     as the engagement proceeds)
/docs
  01-assessment/    Modernization assessment scorecard and findings
  02-plan/          Scope, milestones, acceptance criteria, decision log
  03-outcome/       What changed, verification results, before/after
                     comparison, lessons learned
LICENSE.txt         Microsoft Public License (Ms-PL)
NOTICE.md           Provenance and license basis
```

## Following along

The commit history is the primary evidence trail — each modernization
step is a small, independently reviewable commit tied to a milestone
in `docs/02-plan/`. The `docs/` directory tells the story; the commits
prove it happened as described.
