# Production Update Guide — Updating the School Management System from GitHub

> **Document:** `PRODUCTION_UPDATE_FROM_GITHUB.md`  
> **Repository:** `/opt/sms/app`  
> **Production Compose File:** `docker/docker-compose.prod.yml`  
> **Target Audience:** System administrators who are not expert developers

---

## 1. What This Update Does

This update fixes the following production issues:

1. **Authentication refresh-loop** — The frontend Axios interceptor previously retried token refresh on every 401 response, causing hundreds of simultaneous requests that triggered the rate limiter (429 responses) and banned the client IP for 15 minutes.
2. **API version consistency** — The frontend now uses `/api/v1` as the consistent API base path (previously mixed `/api` and `/api/v1`).
3. **Docker build fix** — The frontend Docker image now receives `VITE_API_URL=/api/v1` as a build argument, so the frontend bundle correctly targets the versioned API.
4. **Enrollment service typo fix** — Fixed `returining` → `returning-user` and removed double-prefixed paths (`/api/v1/api/v1/enrollment/...` → `/api/v1/enrollment/...`).

---

## 2. Prerequisites

- SSH access to the production server (Debian 13)
- `sudo` or root access
- Git installed
- Docker 24+ and Docker Compose v2+ installed
- The repository already cloned at `/opt/sms/app`
- Production `.env` file present at `/opt/sms/app/.env`

---

## 3. Connect to the Server

```bash
ssh admin@<production-server-ip>
sudo -i
```

---

## 4. Check the Current Deployment

```bash
cd /opt/sms/app

# Check current git state
git status
git log --oneline -5

# Check current containers
docker compose -f docker/docker-compose.prod.yml ps

# Check container health
docker compose -f docker/docker-compose.prod.yml ps --health
```

---

## 5. Back Up the Database

**IMPORTANT: Do not skip the backup. If the update fails, you need this backup to restore.**

```bash
# Create backup directory
mkdir -p /var/backups/sms

# Dump the database
docker compose -f docker/docker-compose.prod.yml exec -T postgres \
  pg_dump -U sms_user -d SchoolManagementSystem -F c \
  -f /var/backups/sms/pre-update-$(date +%Y%m%d).dump

# Verify the backup file exists
ls -lh /var/backups/sms/pre-update-*.dump
```

---

## 6. Back Up Important Configuration

```bash
# Back up the current .env file
cp /opt/sms/app/.env /opt/sms/app/.env.backup-$(date +%Y%m%d)

# Back up the current docker-compose file
cp /opt/sms/app/docker/docker-compose.prod.yml \
   /opt/sms/app/docker/docker-compose.prod.yml.backup-$(date +%Y%m%d)
```

---

## 7. Check the Current Git Branch

```bash
cd /opt/sms/app
git branch -v
git remote -v
```

The active branch should be `main`. The remote should be `origin` pointing to `https://github.com/hudson254/SchoolManagementSystem.git`.

---

## 8. Fetch the Latest Changes

```bash
cd /opt/sms/app
git fetch origin main
```

---

## 9. Review the Incoming Commits

```bash
# Show commits that will be applied
git log HEAD..origin/main --oneline

# Show the full diff
git diff HEAD..origin/main --stat
```

Optionally inspect the actual changes:

```bash
git diff HEAD..origin/main -- frontend/sms-web/src/services/api.ts
git diff HEAD..origin/main -- frontend/sms-web/src/services/enrollment.service.ts
git diff HEAD..origin/main -- docker/Dockerfile.frontend
git diff HEAD..origin/main -- docker/docker-compose.prod.yml
git diff HEAD..origin/main -- .env.example
git diff HEAD..origin/main -- frontend/sms-web/.env.example
```

---

## 10. Pull the Changes

```bash
cd /opt/sms/app
## 13. Rebuild Production Docker Images

**Do NOT delete Docker volumes. Do NOT run `docker compose down -v`.**

```bash
cd /opt/sms/app

# Rebuild the frontend image (the code changes require a rebuild)
docker compose -f docker/docker-compose.prod.yml build frontend

# Rebuild the API image if backend code changed
docker compose -f docker/docker-compose.prod.yml build api

# Or rebuild all changed services at once:
docker compose -f docker/docker-compose.prod.yml build
```

The frontend build will now correctly embed `VITE_API_URL=/api/v1` into the JavaScript bundle.

---

## 14. Recreate the Updated Containers

```bash
cd /opt/sms/app

# Recreate only the changed services
docker compose -f docker/docker-compose.prod.yml up -d frontend
docker compose -f docker/docker-compose.prod.yml up -d api

# Or recreate all services at once:
docker compose -f docker/docker-compose.prod.yml up -d
```

**Do NOT run `docker compose down` unless necessary.** If you do need to stop services, only stop the ones being updated:

```bash
# Safer approach — recreate only what changed
docker compose -f docker/docker-compose.prod.yml up -d --force-recreate frontend api
```

---

## 15. Run Database Migrations (if required)

The API automatically applies pending migrations on startup. Check the API logs to confirm:

```bash
docker compose -f docker/docker-compose.prod.yml logs api --tail 50 | grep -i migration
```

If manual migration is required:

```bash
docker compose -f docker/docker-compose.prod.yml exec api dotnet SMS.API.dll migrate-database
```

---

## 16. Check Container Health

```bash
docker compose -f docker/docker-compose.prod.yml ps
```

Expected: All services should show "Up" and "healthy" (postgres, api, frontend, nginx, prometheus, grafana, alertmanager, etc.).

If any container is unhealthy, check its logs:

```bash
docker compose -f docker/docker-compose.prod.yml logs <unhealthy-service-name> --tail 100
```

---

## 17. Test the Website

```bash
# Test the frontend loads (replace with your production domain)
curl -k https://localhost/

# Check the response includes the HTML title
curl -k https://localhost/ | grep -i '<title'
```

---

## 18. Test Login

```bash
# Test API health
curl -k https://localhost/health | python3 -m json.tool

# Test login
curl -k -X POST https://localhost/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"identifier":"admin@school.com","password":"YourSecurePassword123!"}' \
  -c /tmp/cookies.txt

# Check that Set-Cookie headers are present
grep -c "access_token" /tmp/cookies.txt
```

---

## 19. Check API Logs

```bash
# Watch API logs for errors
docker compose -f docker/docker-compose.prod.yml logs api --tail 100 -f

# Look for critical issues
docker compose -f docker/docker-compose.prod.yml logs api --tail 500 | grep -iE 'error|fail|401|403|429|500|exception'
```

Expected: No repeated 401, 429, or 500 errors during normal usage.

---

## 20. Check Nginx Logs

```bash
# Watch nginx logs for errors
docker compose -f docker/docker-compose.prod.yml logs nginx --tail 100 -f

# Look for 4xx/5xx patterns
docker compose -f docker/docker-compose.prod.yml logs nginx --tail 500 | grep -E '" 4[0-9][0-9] |" 5[0-9][0-9] '
```
git pull origin main
```

## 21. Verify `/health`

```bash
curl -k https://localhost/health | python3 -m json.tool
```

Expected response similar to:

```json
{
  "status": "Healthy",
  "duration": ...
}
```

---

## 22. Verify `/metrics`

```bash
curl -k https://localhost/metrics | head -20
```

Expected: Prometheus metrics output starting with `# HELP` and `# TYPE` comments.

---

## 23. Roll Back if the Update Fails

If the update causes problems, roll back immediately:

```bash
cd /opt/sms/app

# Restore the previous .env
cp /opt/sms/app/.env.backup-YYYYMMDD /opt/sms/app/.env

# Revert the code
git checkout <previous-commit-hash>

# Rebuild
docker compose -f docker/docker-compose.prod.yml build

# Restart
docker compose -f docker/docker-compose.prod.yml up -d
```

---

## 24. Restore the Database (if required)

**Only if the update corrupted the database:**

```bash
# Stop the API service
docker compose -f docker/docker-compose.prod.yml stop api

# Drop and recreate the database
docker compose -f docker/docker-compose.prod.yml exec postgres \
  psql -U sms_user -d postgres -c "DROP DATABASE IF EXISTS SchoolManagementSystem;"

docker compose -f docker/docker-compose.prod.yml exec postgres \
  psql -U sms_user -d postgres -c "CREATE DATABASE SchoolManagementSystem OWNER sms_user;"

# Restore from backup
docker compose -f docker/docker-compose.prod.yml exec -T postgres \
  pg_restore -U sms_user -d SchoolManagementSystem \
  < /var/backups/sms/pre-update-YYYYMMDD.dump

# Restart the API
docker compose -f docker/docker-compose.prod.yml start api
```

---

## 25. Verify the Final Git Commit

```bash
cd /opt/sms/app
git log --oneline -3
git status
```

The working tree should be clean (except for the `.env` file which is intentionally git-ignored).

---

## 26. Confirm the Production Deployment is Complete

Run the final verification checklist:

```bash
echo "=== Production Deployment Verification ==="
echo "1. Git commit:" && git log --oneline -1
echo "2. Container status:" && docker compose -f docker/docker-compose.prod.yml ps --health | grep -E 'postgres|api|frontend|nginx'
echo "3. API health:" && curl -s -o /dev/null -w "%{http_code}" https://localhost/health
echo ""
echo "4. Frontend:" && curl -s -o /dev/null -w "%{http_code}" https://localhost/
echo ""
echo "5. Login test:" && curl -s -k -X POST https://localhost/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"identifier\":\"admin@school.com\",\"password\":\"<password>\"}" \
  -c /tmp/cookies.txt -o /dev/null -w "%{http_code}"
echo ""
```

---

## Safety Warnings

| ⚠️ | **Never do the following** |
|----|---------------------------|
| ❌ | Delete Docker volumes (`docker compose down -v`) |
| ❌ | Delete the PostgreSQL container volume |
| ❌ | Overwrite `.env` with `.env.example` |
| ❌ | Commit `.env` to Git |
| ❌ | Expose secrets in terminal output or screenshots |
| ❌ | Force-push Git (`git push --force`) |
| ❌ | Perform destructive database operations without backup |
| ❌ | Proceed with deployment if the backup fails |

---

## Related Documentation

- [Deployment Guide](04-Deployment/README.md) — Full deployment procedures
- [Installation Guide](03-Installation/README.md) — Initial installation
- [Debian 13 Server Preparation Guide](DEBIAN13_SERVER_PREPARATION_GUIDE.md) — Complete server setup
- [Troubleshooting Guide](16-Troubleshooting/README.md) — Deployment troubleshooting
- [Backup and Recovery](14-Backup-and-Recovery/README.md) — Database backup procedures
---

## 11. Verify `.env` Remains Intact

**The `.env` file is in `.gitignore` and will NOT be overwritten by `git pull`.**
**Never run `git checkout -- .env` or restore `.env` from `.env.example`.**

```bash
# Verify .env is still present
ls -la .env

# Compare with backup
diff /opt/sms/app/.env /opt/sms/app/.env.backup-$(date +%Y%m%d)
```

---

## 12. Compare `.env.example` with `.env`

```bash
diff .env.example .env
```

Key changes in this update:
- `API_URL` changed from `/api` to `/api/v1` in `.env.example`

If your `.env` still has `API_URL=/api`, update it:

```bash
# Edit .env with your preferred editor
nano .env
# Change: API_URL=/api → API_URL=/api/v1
```

Or use sed:

```bash
sed -i 's|^API_URL=.*|API_URL=/api/v1|' .env
```