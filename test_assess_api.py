# -*- coding: utf-8 -*-
import base64, subprocess

LOGIN_JSON = '{"Identifier":"hwainaina@kws.go.ke","Password":"nnku2YjDrv6qTw155Jxs4qARdVKAoDw0!"}'
b64 = base64.b64encode(LOGIN_JSON.encode()).decode()

bash = r"""
set -u
echo "$B64" | base64 -d > /tmp/login.json
RAW=$(curl -s -m 20 -D /tmp/hdrs.txt -X POST http://localhost:5000/api/v1/auth/login -H "Content-Type: application/json" --data-binary @/tmp/login.json)
echo "=== LOGIN BODY (first 200) ==="
echo "$RAW" | head -c 200; echo
TOK=$(grep -i '^set-cookie: access_token=' /tmp/hdrs.txt | head -1 | sed 's/^[Ss]et-[Cc]ookie: access_token=//' | cut -d';' -f1)
echo "TOKEN_LEN=${#TOK}"
if [ -z "$TOK" ]; then
  echo "!! no token; headers:"; cat /tmp/hdrs.txt
  exit 1
fi
test_ep() {
  CODE=$(curl -s -m 20 -o /tmp/out.json -w "%{http_code}" -H "Authorization: Bearer $TOK" "http://localhost:5000/api/v1/$1")
  echo "=== GET /api/v1/$1 -> $CODE ==="
  head -c 700 /tmp/out.json; echo
}
test_ep "assessment/types"
test_ep "assessment/grading-scales"
test_ep "assessment/grading-scales/00000000-0000-0000-0000-000000000001"
test_ep "assessment/reports/grade-distribution/00000000-0000-0000-0000-000000000001"
test_ep "assessment/audit-log"
test_ep "assessment/student/517e7581-0994-4b2e-b500-2a75cf91d6b3/results"
"""
bash = bash.replace("$B64", b64)

rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=120)
print("RC:", rc.returncode)
print(rc.stdout[-10000:])
if rc.stderr.strip():
    print("--- STDERR ---")
    print(rc.stderr[-1500:])