# -*- coding: utf-8 -*-
import json, ssl, urllib.request, urllib.error, subprocess

STUDENT = "41fb549a-3639-45f6-a5ee-5cd49b81501b"
BASE = "https://192.168.110.161"
ctx = ssl.create_default_context(); ctx.check_hostname = False; ctx.verify_mode = ssl.CERT_NONE

# delete eligibility row to force recompute
bash = r"""
DB=SchoolManagementSystem
docker exec sms-postgres psql -U sms_user -d $DB -c "SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false); delete from \"StudentCertificateEligibilities\" where \"StudentId\"='41fb549a-3639-45f6-a5ee-5cd49b81501b';"
"""
subprocess.run(["ssh", "-o", "BatchMode=yes", "sms_admin@192.168.110.161", bash],
               capture_output=True, text=True, timeout=60)
print("deleted eligibility row")

data = json.dumps({"Identifier": "hwainaina@kws.go.ke", "Password": "nnku2YjDrv6qTw155Jxs4qARdVKAoDw0!"}).encode()
req = urllib.request.Request(BASE + "/api/v1/auth/login", data=data, headers={"Content-Type": "application/json"}, method="POST")
with urllib.request.urlopen(req, timeout=30, context=ctx) as resp:
    cookies = resp.headers.get_all("Set-Cookie") or []
tok = None
for c in cookies:
    if c.startswith("access_token="):
        tok = c.split("=", 1)[1].split(";")[0]

# force recompute via eligibility endpoint (row is null -> recomputes)
req = urllib.request.Request(BASE + f"/api/v1/assessment/certificate-eligibility/student/{STUDENT}",
                             headers={"Authorization": "Bearer " + tok}, method="GET")
with urllib.request.urlopen(req, timeout=40, context=ctx) as resp:
    print("ELIGIBILITY (recomputed):", resp.status)
    print(resp.read().decode()[:500])