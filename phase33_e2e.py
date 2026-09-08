# -*- coding: utf-8 -*-
"""Phase 33 Final Acceptance Test - run against production API (idempotent)."""
import json, ssl, urllib.request, urllib.error, sys

BASE = "https://192.168.110.161"
ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE
COURSE = "8bb09bd9-d8d3-40ea-a1ed-3987852b2f7d"
STUDENT = "41fb549a-3639-45f6-a5ee-5cd49b81501b"
UNIT_CODE = "CTU101"

def call(method, path, body=None, token=None, timeout=40):
    data = None
    headers = {"Accept": "application/json"}
    if body is not None:
        data = json.dumps(body).encode()
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=timeout, context=ctx) as resp:
            raw = resp.read().decode("utf-8", "replace")
            return resp.status, raw
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8", "replace")
        return e.code, raw

def jget(st, raw):
    try:
        return st, json.loads(raw)
    except Exception:
        return st, raw

print("========== PHASE 33 ACCEPTANCE TEST ==========")

# 0) Login
data = json.dumps({"Identifier": "hwainaina@kws.go.ke",
                   "Password": "nnku2YjDrv6qTw155Jxs4qARdVKAoDw0!"}).encode()
req = urllib.request.Request(BASE + "/api/v1/auth/login", data=data,
                             headers={"Content-Type": "application/json"}, method="POST")
with urllib.request.urlopen(req, timeout=30, context=ctx) as resp:
    cookies = resp.headers.get_all("Set-Cookie") or []
TOK = None
for c in cookies:
    if c.startswith("access_token="):
        TOK = c.split("=", 1)[1].split(";")[0]
assert TOK, "login failed"
print("STEP 0 login: OK")

# 1) Types
st, raw = call("GET", "/api/v1/assessment/types", token=TOK)
st, types = jget(st, raw)
assert st == 200, f"types failed {st}"
type_map = {t["code"]: t["id"] for t in types}
print("STEP 1 assessment types: OK -", len(types))

# 2) Unit (idempotent)
unit_id = None
st, raw = call("GET", "/api/v1/units", token=TOK)
st, units = jget(st, raw)
items = units.get("items", []) if isinstance(units, dict) else (units or [])
for u in items:
    if u.get("code") == UNIT_CODE:
        unit_id = u["id"]
        print("  reusing existing unit", unit_id)
        break
if not unit_id:
    unit_body = {"name": "Phase 33 Controlled Test Unit", "code": UNIT_CODE,
                 "description": "Controlled E2E acceptance unit",
                 "credits": 3, "contactHours": 3, "semester": 1, "courseId": COURSE}
    st, raw = call("POST", "/api/v1/units", unit_body, token=TOK)
    st, unit = jget(st, raw)
    assert st in (200, 201), f"unit create failed {st}: {raw[:300]}"
    unit_id = unit["id"]
print("STEP 2 unit:", unit_id)

# 3) Assessments (skip existing names)
assessments = [
    ("Assignment 1", "ASSIGNMENT", 10),
    ("Assignment 2", "ASSIGNMENT", 15),
    ("CAT", "CAT", 15),
    ("Project", "PROJECT", 20),
    ("Final Examination", "FINALEXAM", 40),
]
a_ids = {}
st, raw = call("GET", f"/api/v1/assessment/unit/{unit_id}", token=TOK)
st, existing = jget(st, raw)
existing_a = {a["name"]: a["id"] for a in (existing or [])}
for name, code, weight in assessments:
    if name in existing_a:
        a_ids[name] = existing_a[name]
        print(f"  reuse {name}: {a_ids[name]}")
        continue
    body = {"name": name, "description": name, "assessmentTypeId": type_map[code],
            "unitId": unit_id, "weight": weight, "maxMarks": 100}
    st, raw = call("POST", "/api/v1/Assessment", body, token=TOK)
    st, a = jget(st, raw)
    assert st in (200, 201), f"assessment create failed {st}: {raw[:300]}"
    a_ids[name] = a["id"]
    print(f"  created {name}: {a['id']} weight={a.get('weight')}")
print("STEP 3 assessments:", len(a_ids))

# 4) Weights validation
st, raw = call("GET", f"/api/v1/assessment/unit/{unit_id}/weights", token=TOK)
st, wv = jget(st, raw)
print("STEP 4 weights ->", st, "isValid:", wv.get("isValid"), "total:", wv.get("totalWeight"))

# 5) Enter marks
marks = {"Assignment 1": 85, "Assignment 2": 70, "CAT": 80, "Project": 90, "Final Examination": 75}
mark_ids = {}
for name, score in marks.items():
    body = {"assessmentId": a_ids[name], "studentId": STUDENT, "score": score,
            "maxScore": 100, "isDraft": False}
    st, raw = call("POST", "/api/v1/Assessment/marks", body, token=TOK)
    if st in (200, 201):
        m = json.loads(raw)
        mark_ids[name] = m["id"]
        print(f"  mark {name} = {score} -> {m['id']}")
    else:
        print(f"  mark {name} = {score} -> {st}: {raw[:150]}")
print("STEP 5 marks entered:", len(mark_ids), "/ 5")

# 6) Calculate (per-student - creates the UnitResult directly)
st, raw = call("GET", f"/api/v1/assessment/results/{unit_id}/student/{STUDENT}", token=TOK)
st, result = jget(st, raw)
print("STEP 6 per-student calculate ->", st)
if st == 200 and isinstance(result, dict):
    print("  FINAL:", result.get("finalScore"), "grade:", result.get("finalGrade"),
          "desc:", result.get("gradeDescription"), "passed:", result.get("isPassed"),
          "colour:", result.get("gradeColor"))
else:
    print("  ", raw[:300])

# 7) submit -> approve -> publish
for action in ("submit", "approve", "publish"):
    st, raw = call("POST", f"/api/v1/Assessment/results/{unit_id}/{action}", {}, token=TOK)
    print(f"STEP 7 {action} ->", st, raw[:200])

# 8) Student published results
st, raw = call("GET", f"/api/v1/assessment/student/{STUDENT}/results", token=TOK)
st, sres = jget(st, raw)
print("STEP 8 student results ->", st)
for r in (sres if isinstance(sres, list) else []):
    print("  unit:", r.get("unitName"), "final:", r.get("finalScore"),
          "grade:", r.get("finalGrade"), "desc:", r.get("gradeDescription"),
          "published:", r.get("isPublished"), "eligible:", r.get("isEligibleForCertificate"))
print()
print("DONE")