#!/usr/bin/env bash
# Production state inspection - Phase 1 / Stage 2 verification
set -u
cd /opt/sms/app || exit 1

PG="docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -t -A -c"

echo "=== GIT ==="
git branch --show-current
git rev-parse HEAD
git status --short | head -20

echo "=== MIGRATIONS ==="
$PG 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";'

echo "=== KEY TABLE COLUMNS ==="
$PG "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema='public' AND table_name IN ('Units','Assessments','StudentAssessmentMarks','UnitResults','GradingScales','GradeBands','CertificateRules','GradeChangeHistories','ModerationRecords','AssessmentTypes') ORDER BY table_name, ordinal_position;"

echo "=== SEED COUNTS ==="
$PG "SELECT 'AssessmentTypes', count(*) FROM \"AssessmentTypes\" UNION ALL SELECT 'GradingScales', count(*) FROM \"GradingScales\" UNION ALL SELECT 'GradeBands', count(*) FROM \"GradeBands\" UNION ALL SELECT 'CertificateRules', count(*) FROM \"CertificateRules\" UNION ALL SELECT 'UnitResults', count(*) FROM \"UnitResults\" UNION ALL SELECT 'Assessments', count(*) FROM \"Assessments\" UNION ALL SELECT 'StudentAssessmentMarks', count(*) FROM \"StudentAssessmentMarks\" UNION ALL SELECT 'ModerationRecords', count(*) FROM \"ModerationRecords\" UNION ALL SELECT 'GradeChangeHistories', count(*) FROM \"GradeChangeHistories\";"

echo "=== CTU101 unit + related ==="
$PG "SELECT u.\"UnitCode\", u.\"UnitName\", u.\"Id\" FROM \"Units\" u WHERE u.\"UnitCode\" = 'CTU101';"
$PG "SELECT a.\"Name\", a.\"MaxScore\", a.\"Weight\", a.\"Status\" FROM \"Assessments\" a JOIN \"Units\" u ON u.\"Id\" = a.\"UnitId\" WHERE u.\"UnitCode\" = 'CTU101';"
$PG "SELECT count(*) FROM \"StudentAssessmentMarks\" m JOIN \"Assessments\" a ON a.\"Id\" = m.\"AssessmentId\" JOIN \"Units\" u ON u.\"Id\" = a.\"UnitId\" WHERE u.\"UnitCode\" = 'CTU101';"

echo "=== ASSESSMENT TYPES ==="
$PG 'SELECT "Name" FROM "AssessmentTypes" ORDER BY 1;'

echo "=== GRADE BANDS ==="
$PG 'SELECT gs."Name" AS scale, gb."GradeLetter", gb."MinPercentage", gb."MaxPercentage", gb."Description" FROM "GradeBands" gb JOIN "GradingScales" gs ON gs."Id" = gb."GradingScaleId" ORDER BY gb."MinPercentage" DESC;'

echo "=== CERT RULE ==="
$PG 'SELECT "MinimumOverallPercentage", "MinimumPassingGrade", "RequireAllRequiredUnits", "IsActive" FROM "CertificateRules" LIMIT 5;'

echo "=== API HEALTH ==="
curl -sk -o /dev/null -w 'api-health:%{http_code}\n' https://127.0.0.1/health