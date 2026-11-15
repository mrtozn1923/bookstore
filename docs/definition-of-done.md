# Definition of Done

A backlog item is done when:

- The acceptance criteria pass as automated tests on the pull request build.
- Code review is done by someone who did not write the code.
- Threat model check in the PR template is answered; a new or changed data flow is in `threat-model/`.
- Every ASVS requirement the story names has a test (`[Trait("asvs", "v5.0.0-x.y.z")]`) or an entry with
  `"verification": "review"` and its evidence in `docs/security/asvs-requirements.json`.
- No new High or Critical finding from the pipeline's security scans, or an exception with an owner and a date.
- Security-relevant events of the story are logged (see `docs/security/authorization-rules.md`).
