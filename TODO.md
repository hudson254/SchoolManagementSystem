# RISK-06 Repair — Implementation Steps

- [x] 1. Establish as-found checkpoint (build + full test suite) — done: build 0 errors; UnitTests 85/85, ApiTests 28/28, IntegrationTests 21/21
- [x] 2. Review remaining Docker/deployment files — findings: seed-data.sql empty (app seeds via `seed-data` command); no Prometheus metrics endpoint in API; Dockerfile.nginx copies nginx-frontend.conf → conf.d; nginx.conf owns 80/443 servers; no pgcrypto/uuid extensions needed
- [x] 3. Create docker/init-db.sql (no CREATE DATABASE/TABLE/seed — single source rule: app seed-data command; pgcrypto + Africa/Nairobi TZ)
- [x] 4. Create docker/nginx-frontend.conf (server block only; preserves /api, /hub, /swagger, /health proxying + SPA fallback + websocket)
- [x] 5. Create docker/prometheus.yml (API has NO metrics endpoint → /health infrastructure probe, documented)
- [x] 6. Create docker/grafana-datasources/datasource.yml (uid prometheus-sms, http://prometheus:9090, isDefault)
- [x] 7. Create docker/grafana-dashboards/dashboard-provider.yml + sms-infrastructure.json (non-empty dir, loads without warnings)
- [x] 8. Validate all bind mounts across all 3 compose files — all referenced files/dirs now present; 0 broken mounts
- [x] 9. Validate compose files — Docker CLI v29.6.1 present but daemon offline (npipe unavailable); static validation substituted; limitation documented in REPAIR_PROGRESS.md Blockers log
- [x] 10. Re-run dotnet build → Build succeeded in 16.9s, 0 errors
- [x] 11. Update REPAIR_PROGRESS.md (RISK-06 → Fixed + changelog entry + files touched)
- [x] 12. Session summary changelog entry appended (10/31 Fixed ~32%, 0 In Progress, 21 Not Started; next item RISK-08)
