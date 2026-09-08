# -*- coding: utf-8 -*-
import subprocess

bash = r"""
echo "=== UnitResults lines (excluding stack) ==="
docker logs sms-api --since 3m 2>&1 | grep -i "UnitResults" | grep -vE "at Npgsql|at Microsoft|at SMS|at System" | tail -15
echo
echo "=== raw tail 130 lines ==="
docker logs sms-api --since 3m 2>&1 | tail -130
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=90)
print(rc.stdout[-9500:])