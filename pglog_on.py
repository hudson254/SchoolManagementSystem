# -*- coding: utf-8 -*-
import subprocess

bash = r"""
echo "=== enable postgres statement logging ==="
docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -c "ALTER SYSTEM SET log_statement = 'all';" 2>&1
docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -c "SHOW log_statement;" 2>&1 | tail -2
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=60)
print(rc.stdout[-1500:])