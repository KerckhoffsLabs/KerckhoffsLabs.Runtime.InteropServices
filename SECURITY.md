# Security policy

## Scope

In scope:

- the `KerckhoffsLabs.Runtime.InteropServices` package, in particular anything that makes a value cross
  the native boundary with the wrong width or layout, or silently truncate where a checked conversion
  should throw;
- the workflows that build and publish the package.

Out of scope:

- native libraries called through the package's types: report those to their maintainers;
- findings that require an already-compromised host;
- code under `src/KerckhoffsLabs.Runtime.InteropServices.Tests` and `tests/`, which does not ship,
  unless it compromises the release pipeline.

## Reporting a vulnerability

**Do not open a public issue, discussion or pull request for a security problem.**

Report it privately through GitHub's
[private vulnerability reporting](https://github.com/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices/security/advisories/new)
(Security → Advisories → *Report a vulnerability*). It is the only channel: reports by email are not
accepted.

Please include:

- the affected package version, .NET version, operating system and architecture;
- a description and, where possible, a minimal reproduction;
- the impact you foresee.

## What to expect

- **Acknowledgement** within 10 working days.
- An initial assessment and a severity (CVSS) within 15 working days.
- Coordinated disclosure: the fix and the advisory are published together, with a CVE requested through
  GitHub when one is warranted. You are credited unless you prefer to remain anonymous.

## Supported versions

Only the latest release receives security fixes, as a new release.
