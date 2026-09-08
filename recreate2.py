# -*- coding: utf-8 -*-
import subprocess

bash = r"""
cd /opt/sms/app
docker compose -f docker/docker-compose.prod.yml up -d --force-recreate api 2>&1 | tail -4
sleep 35
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Status}}" | grep sms-api
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=120)
print(rc.stdout[-2500:])