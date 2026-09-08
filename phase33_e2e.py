# -*- coding: utf-8 -*-
"""Phase 33 Final Acceptance Test - run against production API."""
import json, ssl, urllib.request, urllib.error, sys

BASE = "https://192.168.110.161"
ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE
TOK = None

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
st, raw, cookies = None, None, None
data = json.dumps({"Identifier": "hwainaina@kws.go.ke",
                   "Password": "nnku2YjDrv6qTw155Jxs4qARdVKAoDw0!"}).encode()
req = urllib.request.Request(BASE + "/api/v1/auth/login", data=data,
                             headers={"Content-Type": "application/json"}, method="POST")
with urllib.request.urlopen(req, timeout=30, context=ctx) as resp:
    cookies = resp.headers.get_all("Set-Cookie") or []
for c in cookies:
    if c.startswith("access_token="):
        TOK = c.split("=", 1)[1].split(";")[0]
assert TOK, "login failed"
print("STEP 0 login: OK")

# 1) Assessment types map
st, raw = call("GET", "/api/v1/assessment/types", token=TOK)
st, types = jget(st, raw)
assert st == 200, f"types failed {st}: {raw[:300]}"
type_map = {t["code"]: t["id"] for t in types}
print("STEP 1 assessment types: OK -", len(types), "types")
needed = {"ASSIGNMENT", "CAT", "PROJECT", "FINALEXAM"}
assert needed <= set(type_map), f"missing types {needed - set(type_map)}"

# 2) Create unit
COURSE = "8bb09bd9-d8d3-40ea-a1ed-3987852b2f7d"
unit_body = {"name": "Phase 33 Controlled Test Unit", "code": "CTU101",
             "description": "Controlled E2E acceptance unit",
             "credits": 3, "contactHours": 3, "semester": 1, "courseId": COURSE}
st, raw = call("POST", "/api/v1/units", unit_body, token=TOK)
st, unit = jget(st, raw)
assert st in (200, 201), f"unit create failed {st}: {raw[:400]}"
unit_id = unit["id"]
print("STEP 2 unit created:", unit_id)

# 3) Create 5 assessments
assessments = [
    ("Assignment 1", "ASSIGNMENT", 10),
    ("Assignment 2", "ASSIGNMENT", 15),
    ("CAT", "CAT", 15),
    ("Project", "PROJECT", 20),
    ("Final Examination", "FINALEXAM", 40),
]
a_ids = []
for name, code, weight in assessments:
    body = {"name": name, "description": name, "assessmentTypeId": type_map[code],
            "unitId": unit_id, "weight": weight, "maxMarks": 100}
    st, raw = call("POST", "/api/v1/Assessment", body, token=TOK)
    st, a = jget(st, raw)
    assert st in (200, 201), f"assessment create failed {st}: {raw[:400]}"
    a_ids.append(a["id"])
    print(f"  created {name}: {a['id']} weight={a.get('weight')}")
print("STEP 3 assessments created: OK")

# 4) Validate weights -> must be 100 and valid
st, raw = call("GET", f"/api/v1/assessment/unit/{unit_id}/weights", token=TOK)
st, wv = jget(st, raw)
print("STEP 4 weight validation ->", st, "isValid:", wv.get("isValid"), "total:", wv.get("totalWeight"))

# 5) Enter marks
STUDENT = "41fb549a-3639-45f6-a5ee-5cd49b81501b"
marks = [85, 70, 80, 90, 75]
mark_ids = []
for aid, score in zip(a_ids, marks):
    body = {"assessmentId": aid, "studentId": STUDENT, "score": score,
            "maxScore": 100, "isDraft": False}
    st, raw = call("POST", "/api/v1/Assessment/marks", body, token=TOK)
    st, m = jget(st, raw)
    assert st in (200, 201), f"mark entry failed {st}: {raw[:400]}"
    mark_ids.append(m["id"])
    print(f"  mark {score} -> {m['id']}")
print("STEP 5 marks entered: OK")

# 6) Calculate results
st, raw = call("POST", f"/api/v1/Assessment/results/{unit_id}/calculate", {}, token=TOK, )
st, results = jget(st, raw)
print("STEP 6 calculate ->", st)
if st in (200, 201):
    for r in (results if isinstance(results, list) else [results]):
        print("  FINAL score:", r.get("finalScore"), "grade:", r.get("finalGrade"),
              "desc:", r.get("gradeDescription"), "passed:", r.get("isPassed"))
else:
    print("  response:", raw[:400])

# 7) Submit -> approve -> publish
for action in ("submit", "approve", "publish"):
    st, raw = call("POST", f"/api/v1/Assessment/results/{unit_id}/{action}", {}, token=TOK)
    print(f"STEP 7 {action} ->", st, raw[:200])

# 8) Student visibility + result summary
st, raw = call("GET", f"/api/v1/assessment/student/{STUDENT}/results", token=TOK)
st, sres = jget(st, raw)
print("STEP 8 student results ->", st)
if isinstance(sres, list):
    for r in sres:
        print("  unit:", r.get("unitName"), "final:", r.get("finalScore"),
              "grade:", r.get("finalGrade"), "desc:", r.get("gradeDescription"),
              "published:", r.get("isPublished"))
print()
print("DONE - review outputs above")