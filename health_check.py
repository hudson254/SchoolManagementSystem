# -*- coding: utf-8 -*-
import subprocess, json, ssl, urllib.request

bash = r"""
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}" | grep -E "sms-api|sms-nginx"
docker logs sms-api --since 3m 2>&1 | grep -iE "Starting|migration|CRITICAL|Started|FATAL" | head -5
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=60)
print(rc.stdout[-2000:])

# Public health
ctx = ssl.create_default_context(); ctx.check_hostname = False; ctx.verify_mode = ssl.CERT_NONE
try:
    with urllib.request.urlopen("https://192.168.110.161/health", timeout=20, context=ctx) as r:
        print("PUBLIC HEALTH:", r.status, r.read().decode()[:120])
except Exception as ex:
    print("PUBLIC HEALTH ERROR:", ex)