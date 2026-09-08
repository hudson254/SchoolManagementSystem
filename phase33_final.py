# -*- coding: utf-8 -*-
"""Phase 33 FINAL - clean reset then full workflow acceptance."""
import json, ssl, urllib.request, urllib.error, subprocess

BASE = "https://192.168.110.161"
ctx = ssl.create_default_context(); ctx.check_hostname = False; ctx.verify_mode = ssl.CERT_NONE
UNIT = "b704a982-ca59-4ac5-ace4-5c9594380a2e"
STUDENT = "41fb549a-3639-45f6-a5ee-5cd49b81501b"

def call(method, path, body=None, token=None, timeout=40):
    data = None
    headers = {"Accept": "application/json"}
    if body is not None:
        data = json.dumps(body).encode(); headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=timeout, context=ctx) as resp:
            return resp.status, resp.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")

bash = ("DB=SchoolManagementSystem\n"
        "docker exec sms-postgres psql -U sms_user -d $DB -c \"SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false); update \\\"UnitResults\\\" set \\\"PublicationStatus\\\"=1, \\\"IsPublished\\\"=false, \\\"IsApproved\\\"=false, \\\"PublishedDate\\\"=NULL, \\\"ApprovedDate\\\"=NULL where \\\"UnitId\\\"='b704a982-ca59-4ac5-ace4-5c9594380a2e' and \\\"StudentId\\\"='41fb549a-3639-45f6-a5ee-5cd49b81501b';\"")
subprocess.run(["ssh", "-o", "BatchMode=yes", "sms_admin@192.168.110.161", bash],
               capture_output=True, text=True, timeout=60)
print("reset UnitResult to Draft")

data = json.dumps({"Identifier": "hwainaina@kws.go.ke", "Password": "nnku2YjDrv6qTw155Jxs4qARdVKAoDw0!"}).encode()
req = urllib.request.Request(BASE + "/api/v1/auth/login", data=data, headers={"Content-Type": "application/json"}, method="POST")
with urllib.request.urlopen(req, timeout=30, context=ctx) as resp:
    cookies = resp.headers.get_all("Set-Cookie") or []
tok = None
for c in cookies:
    if c.startswith("access_token="):
        tok = c.split("=", 1)[1].split(";")[0]

def count(path):
    st, raw = call("GET", path, token=tok)
    try:
        return len(json.loads(raw)) if raw else 0
    except Exception:
        return "?"

print("PRE-PUBLISH student results count:", count(f"/api/v1/assessment/student/{STUDENT}/results"), "(expect 0)")

st, raw = call("POST", f"/api/v1/Assessment/results/{UNIT}/submit", {}, token=tok)
print("SUBMIT:", st, raw[:120])
print("POST-SUBMIT (pending) visibility:", count(f"/api/v1/assessment/student/{STUDENT}/results"), "(expect 0)")

st, raw = call("POST", f"/api/v1/Assessment/results/{UNIT}/approve", {}, token=tok)
print("APPROVE:", st, raw[:120])
print("POST-APPROVE visibility:", count(f"/api/v1/assessment/student/{STUDENT}/results"), "(expect 0)")

st, raw = call("POST", f"/api/v1/Assessment/results/{UNIT}/publish", {}, token=tok)
print("PUBLISH:", st, raw[:120])

st, raw = call("GET", f"/api/v1/assessment/student/{STUDENT}/results", token=tok)
print("POST-PUBLISH visibility:", count(f"/api/v1/assessment/student/{STUDENT}/results"), "(expect 1)")
if raw:
    ct = json.loads(raw)
    for r in ct:
        print("   final:", r.get("finalScore"), "grade:", r.get("finalGrade"), "desc:", r.get("gradeDescription"),
              "passed:", r.get("isPassed"), "eligible:", r.get("isEligibleForCertificate"))

st, raw = call("GET", f"/api/v1/assessment/certificate-eligibility/student/{STUDENT}", token=tok)
print("ELIGIBILITY ENDPOINT:", st, raw[:300])