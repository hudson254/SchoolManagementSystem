# -*- coding: utf-8 -*-
import subprocess

bash = r"""
echo "=== EF SQL logging active? (grep INSERT/UPDATE statements) ==="
docker logs sms-api --since 4m 2>&1 | grep -iE "insert into|update \"|RETURNING|Parameter" | grep -viE "at Npgsql|at Microsoft|at SMS" | tail -25
echo
echo "=== current DB state ==="
DB=SchoolManagementSystem
docker exec sms-postgres psql -U sms_user -d $DB -t -c "SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false); select 'marks', count(*) from \"StudentAssessmentMarks\" union all select 'unitresults', count(*) from \"UnitResults\""
echo "=== interceptor source (EF logging hook) ==="
CID=$(docker ps -q -f name=sms-api)
docker exec $CID sh -c "grep -ao 'tenant_id\|set_config\|EnsureTenantContextSet' /app/SMS.Persistence.dll | sort -u | head"
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=90)
print(rc.stdout[-7000:])