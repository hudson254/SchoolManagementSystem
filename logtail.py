# -*- coding: utf-8 -*-
import subprocess

bash = r"""
DB=SchoolManagementSystem
echo "=== delete my manual UnitResult row ==="
docker exec sms-postgres psql -U sms_user -d $DB -c "SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false); delete from \"UnitResults\" where \"id\"='59df53d7-5ee5-4987-8959-6c74cbedf1d2';"
echo "=== full exception block for GetStudentResult ==="
docker logs sms-api --since 15m 2>&1 | grep -A 45 "DbUpdateConcurrencyException" | head -70
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=90)
print(rc.stdout[-7000:])