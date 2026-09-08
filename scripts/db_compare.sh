#!/usr/bin/env bash
cd /opt/sms/app || exit 1
echo '=== units columns ==='
docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c "SELECT column_name FROM information_schema.columns WHERE table_schema='public' AND table_name='units' ORDER BY ordinal_position;"
echo '=== course_offering_units columns ==='
docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c "SELECT column_name FROM information_schema.columns WHERE table_schema='public' AND table_name='course_offering_units' ORDER BY ordinal_position;"
echo '=== CTU101 in units ==='
docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c "SELECT * FROM units LIMIT 10;"
echo '=== Assessments sample with unit join ==='
docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c "SELECT a.\"Title\", a.\"MaxScore\", a.\"Weight\", a.\"Status\", a.\"PublicationStatus\", u.id FROM \"Assessments\" a LEFT JOIN units u ON u.id = a.\"UnitId\" LIMIT 10;"
echo '=== counts ==='
docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c "SELECT count(*) FROM units;"