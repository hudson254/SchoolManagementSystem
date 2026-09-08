# -*- coding: utf-8 -*-
import subprocess

bash = r"""
BUILD_DONE=0
for i in $(seq 1 60); do
  if ! ps -p $1 > /dev/null 2>&1; then BUILD_DONE=1; break; fi
  sleep 15
done
if [ $BUILD_DONE -eq 1 ]; then echo BUILD_FINISHED; else echo BUILD_STILL_RUNNING; fi
tail -4 /tmp/api_build6.log
docker images --format "{{.Repository}}:{{.Tag}} {{.ID}} {{.CreatedAt}}" | grep 'docker-api.*latest'
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", "bash -s", "--", "1427536"],
                    input=bash, capture_output=True, text=True, timeout=300)
print(rc.stdout[-3000:])