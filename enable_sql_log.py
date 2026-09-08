# -*- coding: utf-8 -*-
import base64, subprocess

override = b"""services:
  api:
    ports:
      - "5000:80"
    environment:
      - Logging__Level__Root=Debug
      - Logging__Level__Microsoft.EntityFrameworkCore=Debug
      - Logging__Level__Microsoft.EntityFrameworkCore.Database.Command=Debug
      - Logging__Level__Microsoft.EntityFrameworkCore.SQL=Debug
"""
b64 = base64.b64encode(override).decode()

bash = r"""
echo "$B64" | base64 -d > /opt/sms/app/docker/docker-compose.override.yml
cat /opt/sms/app/docker/docker-compose.override.yml
cd /opt/sms/app
docker compose -f docker/docker-compose.prod.yml up -d api 2>&1 | tail -3
""".replace("$B64", b64)

rc = subprocess.run(["ssh", "-o", "BatchMode=yes",
                     "sms_admin@192.168.110.161", bash],
                    capture_output=True, text=True, timeout=120)
print("RC:", rc.returncode)
print(rc.stdout[-2500:])