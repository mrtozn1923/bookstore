# Security policy

## Reporting a vulnerability

Please do not open a public issue. Use one of these private channels:

- GitHub: the "Report a vulnerability" button on the Security tab of this repository (private vulnerability reporting).
- E-mail: security@bookstore.example.com

Include the affected component and version, the steps to reproduce and the impact you expect.
Turkish or English is fine.

## What happens next

| Step | Target |
|---|---|
| We confirm that we received the report | 3 business days |
| First assessment: valid or not, severity, affected versions | 10 business days |
| Fix released: Critical / High | 14 days / 30 days after the assessment |
| Fix released: Medium / Low | Next planned release, at most 90 days |
| Public advisory | When the fix is available, on a date agreed with you |

We keep you informed at least every 14 days and credit you in the advisory if you want.

## Supported versions

| Component | Supported |
|---|---|
| Bookstore.Api (bookstore.example.com) | Current production release |
| Bookstore.Contracts NuGet package | 2.1.x; 2.0.x until 2027-03-31 |

## Safe harbor

If you act in good faith under this policy, we will not take legal action against you and we will ask others
not to. Good faith means: you test only against your own accounts or the test environment we name, you do not
access, change or keep other people's data beyond what is needed to show the problem, you do not degrade the
service (no denial-of-service, no automated scanning against production), and you give us reasonable time to fix
the problem before you publish it.

## Out of scope

Reports from automated scanners without a demonstrated impact, missing security headers without an exploit path,
denial-of-service by volume, social engineering of staff, and physical attacks.
