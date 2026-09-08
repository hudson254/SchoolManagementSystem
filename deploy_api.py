# -*- coding: utf-8 -*-
import subprocess

bash = r"""
cd /opt/sms/app
docker compose -f docker/docker-compose.prod.yml up -d api 2>&1 | tail -3
sleep 35
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep sms-api
docker logs sms-api --since 1m 2>&1 | grep -iE "Starting|migration|CRITICAL|Started" | head -4
"""
rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=120)
print(rc.stdout[-2500:])