# Handling a vulnerability report

Owner: security champion (Emre). Backup: tech lead.

1. Acknowledge within 3 business days. Thank the reporter, name the person who follows up, never argue in the first reply.
2. Triage within 10 business days in the private report / draft advisory:
   reproduce on a local or test environment, decide valid/invalid, score severity, list affected versions.
3. Fix in the temporary private fork of the draft advisory. Commit messages and PR titles stay neutral
   ("Validate ISBN input length"), never "fix ReDoS in IsbnValidator".
4. Request a CVE from the draft advisory (GitHub is the CNA) when the issue affects a published package or users
   who must act.
5. Agree the publication date with the reporter. Release the patched version, then publish the advisory the same day.
6. Tell users: release notes, the advisory link, and for the API a status page note if customers must act.
7. Within 2 weeks: root cause, a regression test, and an update to `threat-model/` if a threat was missing.
