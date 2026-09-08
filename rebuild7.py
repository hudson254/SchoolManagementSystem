# -*- coding: utf-8 -*-
import subprocess

bash = r"""
set -e
cd /opt/sms/app
git reset --hard e7489930ccb88f3d1a2f848c2131d2a583876ab6
git rev-parse HEAD
nohup docker compose -f docker/docker-compose.prod.yml build api > /tmp/api_build7.log 2>&1 &
echo "BUILD_PID $!"
sleep 5
tail -2 /tmp/api_build7.log
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=300)
print(rc.stdout[-2000:])