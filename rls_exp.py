# -*- coding: utf-8 -*-
import subprocess

bash = r"""
DB=SchoolManagementSystem
docker exec sms-postgres psql -U sms_user -d $DB --set=ON_ERROR_STOP=1 << 'EOSQL'
RESET ALL;
BEGIN;
INSERT INTO "UnitResults" ("id","StudentId","UnitId","EnrollmentId","CourseOfferingId","SemesterId","GradingScaleVersionId","FinalPercentage","GradeLetter","GradeDescription","GpaPoints","PublicationStatus","ModerationStatus","IsPublished","IsApproved","IsRecalculated","tenant_id","created_at","updated_at","created_date","is_deleted","row_version")
SELECT gen_random_uuid(),'41fb549a-3639-45f6-a5ee-5cd49b81501b','b704a982-ca59-4ac5-ace4-5c9594380a2e',NULL,NULL,NULL,NULL,79.00,'A','Distinction',4.0,1,1,false,false,true,'11111111-1111-1111-1111-111111111111',now(),now(),now(),false,NULL
RETURNING "id","row_version";
ROLLBACK;
EOSQL
echo "--- scenario B (tenant set) ---"
docker exec sms-postgres psql -U sms_user -d $DB --set=ON_ERROR_STOP=1 << 'EOSQL'
SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false);
BEGIN;
INSERT INTO "UnitResults" ("id","StudentId","UnitId","EnrollmentId","CourseOfferingId","SemesterId","GradingScaleVersionId","FinalPercentage","GradeLetter","GradeDescription","GpaPoints","PublicationStatus","ModerationStatus","IsPublished","IsApproved","IsRecalculated","tenant_id","created_at","updated_at","created_date","is_deleted","row_version")
SELECT gen_random_uuid(),'41fb549a-3639-45f6-a5ee-5cd49b81501b','b704a982-ca59-4ac5-ace4-5c9594380a2e',NULL,NULL,NULL,NULL,79.00,'A','Distinction',4.0,1,1,false,false,true,'11111111-1111-1111-1111-111111111111',now(),now(),now(),false,NULL
RETURNING "id","row_version";
ROLLBACK;
EOSQL
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=90)
print("RC:", rc.returncode)
print(rc.stdout[-4000:])
print("STDERR:", rc.stderr[-1500:])