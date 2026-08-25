# Provenance

This repository's baseline (pre-modernization) source is the **NerdDinner**
sample application, originally created by Scott Hanselman, Scott Guthrie,
Phil Haack, Rob Conery, and the ASP.NET MVC team, and first published at
`nerddinner.codeplex.com` circa 2008-2009 as a companion sample to an early
ASP.NET MVC tutorial.

## License basis

The original CodePlex project was released under the **Microsoft Public
License (Ms-PL)**, an OSI-approved permissive license. This is documented
directly by Scott Hanselman, who announced the release:

> "Next, we're releasing the NerdDinner sample application at
> http://nerddinner.codeplex.com as MS-Pl."
> — Scott Hanselman, ["Free ASP.NET MVC eBook - NerdDinner.com
> Walkthrough"](https://www.hanselman.com/blog/free-aspnet-mvc-ebook-nerddinnercom-walkthrough),
> March 2009

CodePlex was retired in 2017 and the original license file from the
project's source-control history was not preserved in the GitHub mirrors
that carried the codebase forward. This repository's `LICENSE.txt`
reconstructs the Ms-PL license text (sourced from the [OSI's canonical
copy](https://opensource.org/license/ms-pl)) and applies it here on the
basis of the provenance documented above, in compliance with Ms-PL
Section 3(C) and 3(D), which require retaining license and attribution
notices in any redistribution.

## Source mirror

The pre-modernization baseline in this repository was sourced from
[`spboyer/nerddinner-mvc4`](https://github.com/spboyer/nerddinner-mvc4)
("Copy of original ASP.NET MVC Source code for http://nerddinner.com"),
confirmed to be the ASP.NET MVC 4 / .NET Framework 4.5 codebase (as
opposed to other NerdDinner-named repositories under the `aspnet` org and
elsewhere, which are unrelated later rewrites targeting ASP.NET Core).

Note: the original CodePlex commit history did not carry over to any
available GitHub mirror. This repository's history begins fresh from a
single baseline import commit representing the legacy application as of
this sourcing, rather than the application's full multi-year development
history.

## What this repository is

This is an independent practice modernization exercise conducted by
Decimal Zero LLC, applying our engagement methodology (assessment,
planning, verified modernization) to a real, historically significant
legacy .NET Framework codebase. It is not affiliated with, endorsed by,
or sponsored by Microsoft, the original NerdDinner authors, or the .NET
Foundation. Per Ms-PL Section 3(A), no trademark rights are claimed or
implied.
