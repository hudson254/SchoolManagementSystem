# -*- coding: utf-8 -*-
import subprocess

bash = r"""
docker logs sms-api --since 5m 2>&1 | grep -iE "err|exception|fail|assessment" | grep -viE "health|metrics" | tail -50
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=60)
print(rc.stdout[-8000:])
if rc.stderr.strip():
    print("STDERR:", rc.stderr[-2000:])