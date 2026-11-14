# AI policy — Bookstore team

Last reviewed: 2026-11. Owner: tech lead. Reviewed every six months, or when a tool changes its data terms.

## Tools
- Company plans of the approved coding assistants only. No personal accounts or free plans for company code.
- Agents ask before running commands. Auto-approve only inside a container with no credentials.

## Data
- Never in a prompt or an agent's context: secrets, connection strings, production data, customer personal data.
- Example data comes from the seed (Ada, Alan, Grace; Dune, Emma, The Hobbit).

## Work
- The person who opens the pull request owns the code, whoever wrote it.
- Commits with substantial AI help carry the trailer `AI-Assisted: yes`; the PR template asks the same.
- A written plan (docs/plans/) first for work that touches the data model, more than one module, or security.
- AI review is a pre-filter. It never counts as a required approval.

## Extra care
- Authentication, authorization, payments, cryptography: two human approvals, one from someone who knows the area.
- Unclear requirement: clarify it with the Product Owner before asking an agent.

## Learning
- Learning tasks (new area, onboarding): try for 30 minutes first, write the tests yourself.
- Before accepting AI code, be able to explain every line to the reviewer.

## Measuring
- We watch change lead time, change fail rate, deployment rework rate and review time per pull request.
- We do not use lines generated, suggestion acceptance rate or "AI usage %" as targets.
