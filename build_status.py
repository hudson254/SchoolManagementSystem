# -*- coding: utf-8 -*-
import subprocess

bash = r"""
if ps -p 1427536 > /dev/null 2>&1; then
  echo "BUILD_STILL_RUNNING"
  tail -3 /tmp/api_build6.log
else
  echo "BUILD_DONE"
  tail -8 /tmp/api_build6.log
  docker images --format "{{.Repository}}:{{.Tag}} {{.ID}} {{.CreatedAt}}" | grep 'docker-api.*latest'
fi
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=120)
print(rc.stdout[-2500:])