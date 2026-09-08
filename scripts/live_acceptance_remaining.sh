#!/usr/bin/env bash
set -u
cd /opt/sms/app || exit 1
BASE=https://127.0.0.1/api/v1
COOKIES=/tmp/cj_stage2b.txt
rm -f "$COOKIES"
export $(grep -E '^(ADMIN_EMAIL|ADMIN_PASSWORD)=' .env | xargs)
curl -sk -c "$COOKIES" -X POST "$BASE/auth/login" -H 'Content-Type: application/json' -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}" -o /dev/null
# Hit a GET to receive the non-httpOnly XSRF cookie
curl -sk -b "$COOKIES" -c "$COOKIES" "$BASE/assessment/types" -o /dev/null
XSRF=$(grep XSRF-TOKEN "$COOKIES" | awk '{print $7}')
if command -v python3 >/dev/null 2>&1; then
  XSRF=$(python3 -c "import urllib.parse,sys; print(urllib.parse.unquote(sys.argv[1]))" "$XSRF")
else
  XSRF=$(printf '%s' "$XSRF" | sed 's/%2F/\//g; s/%3D/=/g')
fi
echo "XSRF=$XSRF"

CTU_UNIT="b704a982-ca59-4ac5-ace4-5c9594380a2e"
CTU_STUDENT="41fb549a-3639-45f6-a5ee-5cd49b81501b"
ASSESS1=$(docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c "SELECT \"id\" FROM \"Assessments\" WHERE \"UnitId\"='$CTU_UNIT' ORDER BY \"created_date\" LIMIT 1;")
echo "ASSESS1=$ASSESS1"

echo "=== GET certificate eligibility ==="
curl -sk -b "$COOKIES" "$BASE/assessment/student/$CTU_STUDENT/certificate-eligibility" | head -c 600
echo

echo "=== WRITE: duplicate mark must be 409 ==="
curl -sk -b "$COOKIES" -H "X-CSRF-TOKEN: $XSRF" -H 'Content-Type: application/json' -X POST "$BASE/assessment/marks" -d "{\"assessmentId\":\"$ASSESS1\",\"studentId\":\"$CTU_STUDENT\",\"score\":50,\"maxScore\":100,\"isDraft\":false}" -w "\nHTTP %{http_code}\n"

echo "=== WRITE: invalid mark over max must fail (no DB change) ==="
curl -sk -b "$COOKIES" -H "X-CSRF-TOKEN: $XSRF" -H 'Content-Type: application/json' -X POST "$BASE/assessment/marks" -d "{\"assessmentId\":\"$ASSESS1\",\"studentId\":\"$CTU_STUDENT\",\"score\":150,\"maxScore\":100,\"isDraft\":true}" -w "\nHTTP %{http_code}\n"

echo "=== DONE ==="