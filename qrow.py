# -*- coding: utf-8 -*-
import subprocess

bash = r"""
DB=SchoolManagementSystem
docker exec sms-postgres psql -U sms_user -d $DB -x -c "SELECT set_config('app.tenant_id','11111111-1111-1111-1111-111111111111',false); select * from \"UnitResults\";"
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=60)
print(rc.stdout[-6000:])