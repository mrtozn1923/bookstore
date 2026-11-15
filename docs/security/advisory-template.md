# Security advisory template (GitHub repository security advisory)

Fill this in the draft advisory, never in a public issue or a commit message.

- Title: <component>: <weakness> allows <impact>
- Package / ecosystem: NuGet, `Bookstore.Contracts`
- Affected versions: `>= 2.0.0, < 2.1.1`
- Patched versions: `2.1.1`
- Severity: CVSS 4.0 vector and score, plus our own reasoning in one sentence
- Weakness: CWE id
- Credits: reporter name or handle, as they want to be credited

## Summary
One paragraph: what is wrong and who is affected.

## Impact
What an attacker can do, under which conditions.

## Patches
Which version fixes it; how to upgrade (`dotnet add package Bookstore.Contracts --version 2.1.1`).

## Workarounds
What users who cannot upgrade today can do.

## Timeline
Reported, confirmed, fixed, published (dates in UTC).
