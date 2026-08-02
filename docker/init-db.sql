-- School Management System - PostgreSQL 16 initialisation script
-- Runs once on a fresh data volume via /docker-entrypoint-initdb.d/init.sql
-- (mounted from ./init-db.sql in docker-compose*.yml).
--
-- Deliberately does NOT:
--   * create the database (POSTGRES_DB in compose already does that)
--   * create application tables (EF Core migrations own the schema)
--   * seed application data (single source: scripts/seed.sh ->
--     dotnet run --project src/SMS.API -- seed-data)
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- The docker-entrypoint runs this script already connected to POSTGRES_DB,
-- so we target that database via current_database() instead of hardcoding it
-- (identifiers are case-sensitive and %I preserves the actual casing via
-- psql \gexec; unquoted identifiers would be lowercased by PostgreSQL).
SELECT format('ALTER DATABASE %I SET timezone TO ''Africa/Nairobi'';', current_database())
\gexec
SELECT format('ALTER DATABASE %I SET datestyle TO ''ISO, MDY'';', current_database())
\gexec
