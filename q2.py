# -*- coding: utf-8 -*-
import subprocess

bash = r"""
DB=SchoolManagementSystem
docker exec sms-postgres psql -U sms_user -d $DB -t -c "SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false); select \"StudentId\"::text, \"IsPublished\", \"FinalPercentage\" from \"UnitResults\" where \"StudentId\"='41fb549a-3639-45f6-a5ee-5cd49b81501b';"
docker exec sms-postgres psql -U sms_user -d $DB -t -c "SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false); select \"IsActive\", \"RequireAllRequiredUnits\", \"RequireNoOutstandingIncomplete\", \"MinimumPassingPercentage\" from \"CertificateRules\" where \"id\"='00000000-0000-0000-0000-000000000001';"
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=60)
print(rc.stdout[-2000:])