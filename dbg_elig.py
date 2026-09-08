# -*- coding: utf-8 -*-
import subprocess

bash = r"""
DB=SchoolManagementSystem
echo "=== UnitResults (tenant set) ==="
docker exec sms-postgres psql -U sms_user -d $DB -t -c "SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false); select \"StudentId\"::text,\"UnitId\"::text,\"FinalPercentage\",\"GradeLetter\",\"PublicationStatus\",\"IsPublished\",\"IsApproved\" from \"UnitResults\""
echo "=== eligibility rows ==="
docker exec sms-postgres psql -U sms_user -d $DB -t -c "SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false); select \"StudentId\"::text,\"Status\",\"OverallPercentage\",\"HasOutstandingIncomplete\",\"HasFailedRequiredUnits\" from \"StudentCertificateEligibilities\""
echo "=== submit exception ==="
docker logs sms-api --since 5m 2>&1 | grep -B 2 -A 8 "unhandled exception occurred" | tail -25
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=90)
print(rc.stdout[-5000:])
if rc.stderr.strip():
    print("STDERR:", rc.stderr[-800:])