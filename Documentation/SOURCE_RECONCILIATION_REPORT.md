# Source Reconciliation Report — 2026-09-21

Reconciliation of the local working tree, the GitHub repository, and the
production server after OMS Phases 2A–2D. This document records what was
compared, what was retained, and why. It contains no credentials.

## Sources compared

| Source | Version | Notes |
|---|---|---|
| Local working tree | `coordinator-repair` | OMS Phases 2A–2D (backend + React 19/MUI frontend), uncommitted before reconciliation |
| GitHub `origin/main` | `25d2d33` | Strict ancestor of the reconciled branch (fast-forwarded) |
| GitHub `origin/coordinator-repair` | `9cce84f` → `53b5f6f` | Branch pushed with the reconciliation commit |
| Production `/opt/sms/app` (192.168.110.161) | `ec9e06a` + uncommitted assessment fix | Read-only inspection; production was NOT modified |

## Production version identification

Production runs from a Git checkout at `/opt/sms/app`:

* Git HEAD: `ec9e06a` (`feat(materials,assignments,dashboard): assignment docs, study materials, real dashboards`), detached, on the `coordinator-repair` lineage.
* Docker images/containers built from that tree (`sms-api`, `sms-web`, PostgreSQL).
* Two uncommitted production-side files: `frontend/sms-web/src/utils/listShape.ts`,
  `listShape.test.ts` plus the `AssessmentWorkspace.tsx` change that consumes them
  (identical to commit `aa44456`, the SYSTEM ADMINISTRATOR assessment-render fix).

## What was retained

1. **Local (OMS Phases 2A–2D)** — all OMS backend handlers/DTOs/controllers/policies,
   the React 19 + TypeScript + MUI OMS frontend (dashboard, orders list, order
   detail, order creation, service client), OMS permission tiers, and all OMS
   tests. Committed as `53b5f6f` after full local validation.
2. **Production-only fix** — the assessment workspace list-shape normalization
   (`listShape.ts` / `listShape.test.ts` + `AssessmentWorkspace.tsx` usage), which
   fixes a render crash for SYSTEM ADMINISTRATOR when `/units` returns a paginated
   envelope and `/assessment/types` returns a bare array. Recovered from the
   production working tree (commit `aa44456` on `grading-system-repair`) rather
   than copied wholesale.
3. **GitHub** — `origin/main` contained no OMS work and no conflicting changes;
   it is a strict ancestor of the reconciled branch, so it is updated by
   fast-forward only (no force push).

## Rejected / cleaned

* `stage2_alex.bundle` — a 1.9 MB generated git-bundle artifact tracked at the
  repo root (repo precedent: `15c71d4` removed the same artifact). Removed;
  no source references exist.
* No secrets, `.env` files, TLS keys, or production configuration were copied
  from production. Production `.env`, TLS, and Nginx were never read into the
  repository or modified.

## Validation at reconciliation time

* Frontend: `tsc --noEmit` clean; full Vitest suite green (one pre-existing
  timing-sensitive `Register.test.tsx` case can exceed its 5 s timeout under
  heavy parallel load — passes in isolation and in dedicated re-runs);
  `npm run build` succeeds.
* Backend: `dotnet restore` + Release build clean; `SMS.UnitTests` and
  `SMS.ApiTests` suites green; OMS API integration tests green against the
  `docker-compose.oms.yml` PostgreSQL (port 5434) + API container.
* Docker compose configs (`docker-compose.yml`, `docker-compose.prod.yml`,
  `docker-compose.oms.yml`) validate with `docker compose config --quiet`.

## Deployment note

The reconciled source has NOT been deployed to production. Production remains at
`ec9e06a` + the assessment fix files on disk. Deploying is a separate,
explicitly-authorized task.
