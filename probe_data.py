# -*- coding: utf-8 -*-
import json, ssl, urllib.request, urllib.error

BASE = "https://192.168.110.161"
ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

def call(method, path, body=None, token=None, timeout=30):
    url = BASE + path
    data = None
    headers = {"Accept": "application/json"}
    if body is not None:
        data = json.dumps(body).encode()
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=timeout, context=ctx) as resp:
            raw = resp.read().decode("utf-8", "replace")
            cookies = resp.headers.get_all("Set-Cookie") or []
            return resp.status, raw, cookies
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8", "replace")
        return e.code, raw, []

st, raw, cookies = call("POST", "/api/v1/auth/login",
                        {"Identifier": "hwainaina@kws.go.ke",
                         "Password": "nnku2YjDrv6qTw155Jxs4qARdVKAoDw0!"})
tok = None
for c in cookies:
    if c.startswith("access_token="):
        tok = c.split("=", 1)[1].split(";")[0]
print("token:", bool(tok))

for path in ["/api/v1/students", "/api/v1/courses", "/api/v1/units", "/api/v1/course-offerings"]:
    st, raw, _ = call("GET", path, token=tok)
    print("=== GET", path, "->", st, "===")
    print((raw or "")[:700])
    print()