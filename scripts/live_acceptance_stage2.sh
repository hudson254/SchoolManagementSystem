#!/usr/bin/env bash
# Stage 2 live acceptance - deployed production
set -u
cd /opt/sms/app || exit 1

D=$(date +%Y%m%dT%H%M%S)
LOG=/tmp/stage2_acceptance_${D}.log
exec > "$LOG" 2>&1

BASE=https://127.0.0.1/api/v1
COOKIES=/tmp/cj_stage2.txt
rm -f "$COOKIES"

echo "=== DEPLOYED STATE ==="
echo "commit: $(git rev-parse HEAD)"
echo "branch: $(git branch --show-current)"
docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1;"

export $(grep -E '^(ADMIN_EMAIL|ADMIN_PASSWORD)=' .env | xargs)
LOGIN=$(curl -sk -c "$COOKIES" -X POST "$BASE/auth/login" -H 'Content-Type: application/json' -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}")
echo "=== LOGIN ==="
echo "$LOGIN" | head -c 300
echo

CTU_UNIT=$(docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c "SELECT id FROM units WHERE code='CTU101' LIMIT 1;")
CTU_STUDENT=$(docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c "SELECT \"StudentId\" FROM \"UnitResults\" WHERE \"UnitId\"='$CTU_UNIT' LIMIT 1;")
echo "=== CONTROLLED DATA ==="
echo "CTU_UNIT=$CTU_UNIT"
echo "CTU_STUDENT=$CTU_STUDENT"

echo "=== GET assessment types ==="
curl -sk -b "$COOKIES" "$BASE/assessment/types" | head -c 400
echo

echo "=== GET grading scales ==="
curl -sk -b "$COOKIES" "$BASE/assessment/grading-scales" | head -c 400
echo

echo "=== GET weight validation (CTU101) ==="
curl -sk -b "$COOKIES" "$BASE/assessment/unit/$CTU_UNIT/weights" | head -c 400
echo

echo "=== GET grade distribution (CTU101) ==="
curl -sk -b "$COOKIES" "$BASE/assessment/reports/grade-distribution/$CTU_UNIT" | head -c 400
echo

echo "=== GET pass/fail rates (CTU101) ==="
curl -sk -b "$COOKIES" "$BASE/assessment/reports/pass-fail-rates/$CTU_UNIT" | head -c 400
echo

echo "=== GET assessment summary (CTU101) ==="
curl -sk -b "$COOKIES" "$BASE/assessment/reports/assessment-summary/$CTU_UNIT" | head -c 500
echo

echo "=== GET audit log ==="
curl -sk -b "$COOKIES" "$BASE/assessment/audit-log" | head -c 400
echo

echo "=== GET /grades (paged) ==="
curl -sk -b "$COOKIES" "$BASE/grades?pageSize=5" | head -c 500
echo

echo "=== GET /grades/unit/CTU101 ==="
curl -sk -b "$COOKIES" "$BASE/grades/unit/$CTU_UNIT" | head -c 500
echo

echo "=== GET transcript ==="
curl -sk -b "$COOKIES" "$BASE/grades/transcript/$CTU_STUDENT" | head -c 600
echo

echo "=== GET student results (published gating) ==="
curl -sk -b "$COOKIES" "$BASE/assessment/student/$CTU_STUDENT/results" | head -c 600
echo

echo "=== GET certificate eligibility ==="
curl -sk -b "$COOKIES" "$BASE/assessment/student/$CTU_STUDENT/certificate-eligibility" | head -c 500
echo

echo "=== WRITE: duplicate mark must be 409 ==="
ASSESS1=$(docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c "SELECT \"id\" FROM \"Assessments\" WHERE \"UnitId\"='$CTU_UNIT' ORDER BY \"created_date\" LIMIT 1;")
XSRF=$(grep XSRF-TOKEN "$COOKIES" | awk '{print $7}')
curl -sk -b "$COOKIES" -H "X-CSRF-TOKEN: $XSRF" -H 'Content-Type: application/json' -o /dev/null -w "duplicate-mark HTTP %{http_code}\n" -X POST "$BASE/assessment/marks" -d "{\"assessmentId\":\"$ASSESS1\",\"studentId\":\"$CTU_STUDENT\",\"score\":50,\"maxScore\":100,\"isDraft\":false}"

echo "=== FRONTEND SPA route ==="
curl -sk -o /dev/null -w "GET /assessment -> %{http_code}\n" https://127.0.0.1/assessment

echo "=== DONE ==="