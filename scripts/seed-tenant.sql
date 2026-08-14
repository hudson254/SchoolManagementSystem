-- Seed a default tenant required by TenantResolutionMiddleware
-- The Tenant entity extends BaseEntity so it needs the full set of columns.
-- BaseEntity columns use explicit snake_case names (id, tenant_id, created_at, etc.)
-- Tenant's own properties (Name, Organization, etc.) use PascalCase quoted names.
INSERT INTO "Tenants" (
    "id",
    "tenant_id",
    "Name",
    "Organization",
    "Subdomain",
    "IsActive",
    "MaxStudents",
    "MaxLecturers",
    "MaxStorageMB",
    "ThemeColor",
    "created_at",
    "updated_at",
    "created_date",
    "is_deleted"
)
VALUES
    (
        '11111111-1111-1111-1111-111111111111',
        '11111111-1111-1111-1111-111111111111',
        'Default School',
        'Default Organization',
        'default',
        TRUE,
        100,
        50,
        10240,
        '#576426',
        NOW(),
        NOW(),
        NOW(),
        FALSE
    )
ON CONFLICT ("id") DO NOTHING;
