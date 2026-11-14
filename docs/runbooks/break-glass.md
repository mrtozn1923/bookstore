# Runbook: break-glass access

Use when normal access to production does not work (SSO down, VPN down, DNS down) and an incident needs hands on
production. Every use is an incident by itself and gets a postmortem.

## Before (prepared, checked every quarter)
- Two break-glass cloud accounts that do not use SSO; credentials in the password manager's emergency vault, with
  hardware keys kept by two different people (Selin, Emre).
- A printed copy of this page and of `docs/dependencies.md`, because a wiki behind SSO is unreachable when SSO is down.
- Contact numbers for the cloud provider's support, the registrar and the payment provider, outside the company chat.

## Use
1. Say in the incident channel (or by phone if chat is down) that break-glass access is being used, and by whom.
2. Two people: one types, one reads the steps and writes the timeline.
3. Do only what the incident needs; no clean-up while you are in.
4. When done: rotate the break-glass credentials, review the cloud audit log of the session, record it in
   `docs/drills/log.md`.
