> **PROVISIONAL / NOT PRODUCTION READY:** PRODUCTION_DEPLOYMENT_FINAL_REPORT.md
> supersedes verification claims below. A root docker-compose.oms.yml include now
> exists. Use `source ./oms_env.sh` then `oms_test` for the database-backed SMS API
> suite; the last run failed 5/111 tests. This is not full OMS HTTP validation.


# OMS (Order Management System) Integration Guide

Status: verified against the working tree on branch `coordinator-repair`.
Every command in this document was executed; its actual result is stated. Where
something required by the task brief does **not** exist in this repository, that
is stated explicitly instead of assumed (see "OMS feature-code status").

---

## 1. What "OMS" means in this repository

The repository provisions the **OMS storage contract** - the configuration keys,
environment variables, storage paths and Compose wiring for:

| Concern | Configuration key | Environment variable |
|---------|-------------------|----------------------|
| Roads database file | `RoadsDb:File` | `RoadsDb__File` (`ROADS_DB_FILE`) |
| WAL recovery target | `WALRecovery:Target` | `WALRecovery__Target` (`WAL_RECOVERY_TARGET`) |
| WAL recovery path | `WALRecovery:RecoveryPath` | `WALRecovery__RecoveryPath` (`WAL_RECOVERY_PATH`) |
| Order manifest storage on/off | `OrderManifestStorage:Enabled` | `OrderManifestStorage__Enabled` |
| Order manifest base path | `OrderManifestStorage:BasePath` | `OrderManifestStorage__BasePath` (`ORDER_MANIFEST_STORAGE_PATH`) |
| Order manifest file prefix | `OrderManifestStorage:FilePrefix` | `OrderManifestStorage__FilePrefix` |
| Order manifest extension | `OrderManifestStorage:FileExtension` | `OrderManifestStorage__FileExtension` |
| Order manifest retention | `OrderManifestStorage:RetentionDays` | `OrderManifestStorage__RetentionDays` |

### OMS feature-code status (verified, not assumed)

A repository-wide search (`src/`, `tests/`, and all git history) found **no OMS
domain code**: there is no `Order*` entity, controller, handler, service, CSV
importer, order state machine, `RoadAccounting` type, `OrderManifestStorage`
implementation, `ProdStorage` type, `OrderManifestStorageTests` class or
`ProductionStorageLayoutTests` class. The keys above are therefore
*configuration-only*: nothing in `src/` currently reads them and no test
currently asserts their behaviour.

Consequence: "OMS order-state, CSV import, RoadAccounting and order-manifest
**tests**" cannot be executed, because no such tests or features exist in this
codebase; they were not invented here. What *is* verifiable - and was verified -
is the OMS **environment**: Compose validity, container start-up, real
PostgreSQL connectivity, real EF Core migrations, and the OMS storage paths
being mounted, created and writable. See section 6.

---

## 2. Production configuration

### 2.1 Files

| File | Role |
|------|------|
| `src/SMS.API/appsettings.json` | Base configuration (committed) |
| `src/SMS.API/appsettings.Production.json` | Production defaults (**gitignored**; per-deployment file) |
| `docker/docker-compose.prod.yml` | Production Compose (the file in use on the host) |
| `docker/.env` (on the host) | Production values including secrets (**never committed**) |

.NET configuration does **not** expand `${VAR}` placeholders. Defaults live in
`appsettings.Production.json`; the container deployment overrides them with
double-underscore environment variables supplied by the Compose file.

### 2.2 Environment variables by name

Non-secret (safe to document, defaults shown):

```text
ROADS_DB_FILE=/app/data/roads.db
WAL_RECOVERY_TARGET=latest
WAL_RECOVERY_PATH=/app/data/wal-recovery
ORDER_MANIFEST_STORAGE_PATH=/app/data/order-manifests
ORDER_MANIFEST_FILE_PREFIX=order-manifest-
ORDER_MANIFEST_RETENTION_DAYS=30
ORDER_MANIFEST_STORAGE_ENABLED=true
```

Secrets - **names only; values are never documented, logged or committed**:

```text
DB_PASSWORD        (production PostgreSQL password)
JWT_SECRET         (JWT signing key, >= 64 chars)
ADMIN_EMAIL        (initial administrator e-mail, used for seeding)
ADMIN_PASSWORD     (initial administrator password, used for seeding)
FRONTEND_URL       (frontend origin; required in production)
GRAFANA_PASSWORD   (monitoring UI password)
```

Sourcing rules:

* Production secrets come only from the deployment environment - `.env` in the
  Compose project directory (`docker/.env` on the production host) or an
  equivalent secret store. They are never read from `oms_env.sh` and never
  committed.
* Non-secret OMS values may be set in `.env` (see `.env.example`) or omitted,
  because the Compose files supply identical defaults via `${VAR:-default}`.

### 2.3 Compose interpolation pitfall (fixed in this change set)

Compose default syntax is `${VAR:-default}` (**colon-dash**). `${VAR:default}`
is invalid and aborts parsing with `invalid interpolation format for ...`. That
was the defect that made both `docker-compose.oms.yml` and
`docker-compose.prod.yml` unrunnable.

### 2.4 appsettings.Production.json must be valid JSON

A malformed `appsettings.Production.json` is fatal: ASP.NET Core fails during
configuration load and the process exits before it ever listens.

```text
Unhandled exception. System.IO.InvalidDataException: Failed to load configuration
from file '...appsettings.Production.json'.
 ---> System.FormatException: Could not parse the JSON file.
 ---> System.Text.Json.JsonReaderException: ',' is invalid after a single JSON value.
```

Validate it before building an image:

```bash
node -e "JSON.parse(require('fs').readFileSync('src/SMS.API/appsettings.Production.json','utf8'));console.log('VALID')"
```
---

## 3. Storage locations

| Container path | Purpose | Mount (prod / OMS tests) |
|----------------|---------|--------------------------|
| `/app/data` | `roads.db`, `wal-recovery/`, `order-manifests/` | `api_data` / `oms_test_data` |
| `/app/uploads` | user file uploads | `api_uploads` / `oms_test_uploads` |
| `/app/logs` | application logs | `api_logs` / `oms_test_logs` |
| `/app/dataprotection-keys` | ASP.NET DataProtection keys | `api_dataprotection` / `oms_test_dataprotection` |

Do **not** mount a second volume on `/app/data`: a later mount on the same path
silently shadows the earlier one. That is why the previously added
`api_roads_db:/app/data` mount was removed - it would have hidden the existing
production data in `api_data`.

---

## 4. OMS Compose integration

The canonical file is `docker/docker-compose.oms.yml`. Every Compose file in this
repository lives in `docker/`; there is deliberately **no** root-level
`docker-compose.oms.yml` copy, because the file's relative paths
(`./init-db.sql`, `./init-db-rls.sql`, `context: ..`) resolve relative to
`docker/`.

Validate (prints nothing, exits 0):

```bash
docker compose -f docker/docker-compose.oms.yml config --quiet
```

Start, inspect, stop:

```bash
docker compose -f docker/docker-compose.oms.yml up -d --build
docker compose -f docker/docker-compose.oms.yml ps
docker compose -f docker/docker-compose.oms.yml logs --tail 200
docker compose -f docker/docker-compose.oms.yml down
```

`down` intentionally omits `-v`: the named test volumes are kept so test state
survives a down/up cycle.

Two defects this file previously had, both fixed:

1. `${OMS_DB_PASSWORD:testpass123}` - invalid interpolation; Compose refused to
   parse the file. Now `${OMS_DB_PASSWORD:-testpass123}`.
2. Only `init-db.sql` was mounted, but that script runs
   `\i /docker-entrypoint-initdb.d/init-db-rls.sql` and the PostgreSQL entrypoint
   runs init scripts with `ON_ERROR_STOP=1`, so a missing include aborts
   first-time initialisation. `init-db-rls.sql` is now mounted too.

---

## 5. OMS environment script

`oms_env.sh` supplies non-secret test defaults and command helpers. It is safe
to source: strict mode (`set -euo pipefail`) is enabled only when the file is
executed, never when sourced.

```bash
source ./oms_env.sh
oms_check_env      # verify docker/dotnet/compose file
oms_show_env       # effective non-secret configuration (secrets masked)
oms_validate       # docker compose -f docker/docker-compose.oms.yml config --quiet
oms_up             # docker compose -f docker/docker-compose.oms.yml up -d --build
oms_ps
oms_logs
oms_health         # GET http://localhost:5080/health
oms_test           # dotnet test tests/SMS.ApiTests/SMS.ApiTests.csproj --configuration Release
oms_down           # docker compose -f docker/docker-compose.oms.yml down
```

Overridable variables (all non-secret, all optional):
---

## 6. Verification commands

```bash
# Compose validity
docker compose -f docker/docker-compose.oms.yml config --quiet
docker compose -f docker/docker-compose.prod.yml config --quiet

# Real PostgreSQL + real migrations + real application container
docker compose -f docker/docker-compose.oms.yml up -d --build
docker compose -f docker/docker-compose.oms.yml ps
curl --silent --fail http://localhost:5080/health

# Storage paths are volumes (not the ephemeral container layer) and writable
docker exec oms-test-api sh -c 'ls -ld /app/data /app/data/wal-recovery /app/data/order-manifests /app/uploads'
docker exec oms-test-api sh -c 'touch /app/data/order-manifests/.write-probe && rm /app/data/order-manifests/.write-probe && echo WRITABLE'
docker inspect oms-test-api --format '{{range .Mounts}}{{.Type}} {{.Destination}}{{println}}{{end}}'

# API test suite against the OMS Compose PostgreSQL
dotnet test tests/SMS.ApiTests/SMS.ApiTests.csproj --configuration Release
```

---

## 7. Frontend shell, stylesheet and routes

Discovered from the repository (no invented paths):

| Item | Path |
|------|------|
| HTML shell (Vite entry) | `frontend/sms-web/index.html` |
| JS entry point | `frontend/sms-web/src/main.tsx` |
| Root component / router | `frontend/sms-web/src/App.tsx` |
| Stylesheet (imported by `main.tsx`) | `frontend/sms-web/src/index.css` |
| MUI theme | `frontend/sms-web/src/theme.ts` |
| Static assets | `frontend/sms-web/public/` (e.g. `logo.png`) |
| Build output | `frontend/sms-web/dist/` |
| Container static root | `/usr/share/nginx/html` |

Build and route:

```bash
cd frontend/sms-web && npm run build
```

`npm run build` runs `tsc && vite build`. In production nginx serves the SPA from
`/` (`docker/nginx-frontend.conf`: `try_files $uri $uri/ /index.html`,
`error_page 404 /index.html`), proxies `/api/`, `/hub/`, `/swagger/`,
`/uploads/` and `/health` to the API, and the edge proxy (`docker/nginx.conf`)
terminates HTTPS on 443 and forwards `/` to the frontend container.

There is no separate "OMS frontend": no `OrderFormPage`, no order/manifest UI and
no OMS-specific stylesheet exist in `frontend/sms-web/src` (verified by search).
Frontend routing friction reported for an order form cannot be reproduced in
this tree; see the final report's "Remaining issues".

---

## 8. Troubleshooting

| Symptom | Cause | Action |
|---------|-------|--------|
| App appears to "wait" forever or exits instantly with `Failed to load configuration from file ... appsettings.Production.json` / `Could not parse the JSON file` | Malformed JSON in `appsettings.Production.json` | Fix the JSON; validate with the `node -e JSON.parse(...)` command in 2.4 |
| Waiting for PostgreSQL | `depends_on: condition: service_healthy` unmet, or wrong `Host=` in the connection string | `docker compose ps` (expect `postgres` healthy); ensure `ConnectionStrings__DefaultConnection` uses the service name `postgres`, not `localhost` |
| Compose: `required variable X is missing a value` | Required variable absent from the env file | Supply it in `.env` (prod: `docker/.env`) or export it; required names are listed in 2.2 |
| Compose: `invalid interpolation format for ...` | `${VAR:default}` used instead of `${VAR:-default}` | Use the `${VAR:-default}` form |
| `Cors:AllowedOrigins must be configured in production` | Neither `Cors:AllowedOrigins` nor `Frontend:Url` set | Set `FRONTEND_URL` so `Frontend__Url`/`Cors__AllowedOrigins__0` are populated |
| Missing bind mount / storage absent inside container | Volume not declared on the `api` service | Compare `docker inspect <ctr> --format '{{range .Mounts}}...'` with section 3; never add a second mount on `/app/data` |
| Storage permission denied / read-only file system | Volume mounted `:ro`, or files owned by another uid (container runs as `appuser`, uid 1001) | Re-mount read-write; `docker exec -u 0 <ctr> chown -R 1001:1001 /app/data /app/uploads` |
| WAL recovery problems | `WALRecovery__Target`/`RecoveryPath` wrong or path not writable | Verify `WAL_RECOVERY_TARGET` and that `/app/data/wal-recovery` is on the `api_data` volume and writable |
| PostgreSQL connection problems | Container down, port conflict, wrong credentials | `docker compose ps`, `docker logs <pg>`, `pg_isready -U <user> -d <db>`; confirm the password matches `docker/.env` |
| PostgreSQL init aborts on first start | `init-db-rls.sql` not mounted while `init-db.sql` runs `\i` on it | Mount both scripts (fixed in `docker-compose.oms.yml`) |
| Failed health check (`curl .../health` non-zero exit) | Wrong port / API not yet listening / `curl` missing | The API image installs `curl`; verify `ASPNETCORE_URLS` or `Kestrel__Endpoints__Http__Url` and `start_period`; read logs before lengthening intervals |
| `SocketException (10013)` binding port | Binding a privileged port (80) locally without admin rights | Set `Kestrel__Endpoints__Http__Url=http://localhost:<port>`; containers do not have this restriction |

`OMS_DB_NAME`, `OMS_DB_USER`, `OMS_DB_PASSWORD`, `OMS_POSTGRES_PORT`,
`OMS_API_PORT`, `JWT_SECRET`, `FRONTEND_URL`, `ROADS_DB_FILE`,
`WAL_RECOVERY_TARGET`, `WAL_RECOVERY_PATH`, `ORDER_MANIFEST_STORAGE_PATH`,
`ORDER_MANIFEST_FILE_PREFIX`, `ORDER_MANIFEST_RETENTION_DAYS`.
