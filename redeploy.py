# -*- coding: utf-8 -*-
import subprocess

bash = r"""
set -e
cd /opt/sms/app
git reset --hard bc4321ce61148944f9325680b40c87bd13fc69ba
git rev-parse HEAD
echo "=== verify engine fix in source ==="
grep -n "existingResult == null" src/SMS.Infrastructure/Services/AssessmentEngine.cs | head -2
grep -n "existingEligibility == null" src/SMS.Infrastructure/Services/AssessmentEngine.cs | head -2
echo "=== remove temp override (restore baseline; it is deleted in snapshot) ==="
rm -f docker/docker-compose.override.yml
ls docker/docker-compose.override.yml 2>&1 || echo "override removed"
echo "=== clean CONTROLLED test data (CTU101 unit + assessments + marks + results) ==="
DB=SchoolManagementSystem
docker exec sms-postgres psql -U sms_user -d $DB << 'EOSQL'
SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false);
DELETE FROM "UnitResults" WHERE "UnitId"='b704a982-ca59-4ac5-ace4-5c9594380a2e';
DELETE FROM "StudentAssessmentMarks" WHERE "AssessmentId" IN (SELECT "id" FROM "Assessments" WHERE "UnitId"='b704a982-ca59-4ac5-ace4-5c9594380a2e');
DELETE FROM "Assessments" WHERE "UnitId"='b704a982-ca59-4ac5-ace4-5c9594380a2e';
DELETE FROM "Units" WHERE "id"='b704a982-ca59-4ac5-ace4-5c9594380a2e';
SELECT 'unit remains' WHERE EXISTS (SELECT 1 FROM "Units" WHERE "id"='b704a982-ca59-4ac5-ace4-5c9594380a2e');
EOSQL
echo "=== build image ==="
nohup docker compose -f docker/docker-compose.prod.yml build api > /tmp/api_build6.log 2>&1 &
echo "BUILD_PID $!"
sleep 6
tail -2 /tmp/api_build6.log
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=300)
print("RC:", rc.returncode)
print(rc.stdout[-4000:])
print("STDERR:", rc.stderr[-1500:])