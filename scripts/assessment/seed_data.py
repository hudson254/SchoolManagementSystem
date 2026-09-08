# -*- coding: utf-8 -*-
# Seed data + SQL fragments for the assessment grading migration.
# NOTE: The Tenants primary-key column is lowercase "id" (BaseEntity maps
# [Column("id")]); all subqueries reference "id" accordingly.
# All SQL here is expressed as RAW strings with REAL double quotes; the C#
# generator escapes them as \\\" for the migration source.

TENANT_EXPR = r"""COALESCE((SELECT "id" FROM "Tenants" WHERE "IsActive" = true ORDER BY "CreatedDate" LIMIT 1), '00000000-0000-0000-0000-000000000000')"""
SET_TENANT = r"""SELECT set_config('app.tenant_id', COALESCE((SELECT "id"::text FROM "Tenants" WHERE "IsActive" = true ORDER BY "CreatedDate" LIMIT 1), ''), false);"""

TYPES = [
    ("Assignment", "ASSIGNMENT", "Written assignments", 1, 1),
    ("Practical", "PRACTICAL", "Practical exercises", 2, 2),
    ("Laboratory", "LABORATORY", "Lab work", 3, 3),
    ("CAT", "CAT", "Continuous Assessment Test", 4, 4),
    ("Quiz", "QUIZ", "Online quizzes", 5, 5),
    ("Oral Examination", "ORAL", "Oral presentations and exams", 6, 6),
    ("Project", "PROJECT", "Major project work", 7, 7),
    ("Presentation", "PRESENTATION", "Class presentations", 8, 8),
    ("Final Examination", "FINALEXAM", "End-of-term final exam", 9, 9),
    ("Supplementary Examination", "SUPP", "Supplementary retake exam", 10, 10),
    ("Retake Examination", "RETAKE", "Full retake examination", 11, 11),
    ("Coursework", "COURSEWORK", "Overall coursework component", 12, 12),
    ("Participation", "PARTICIPATION", "Class participation and attendance", 13, 13),
]

BANDS = [
    ("00000000-0000-0000-0000-000000000001", "A", "Distinction", "75.00", "100.00", "4.0", "#00AA00", None, 1),
    ("00000000-0000-0000-0000-000000000002", "B", "Credit", "65.00", "74.99", "3.0", "#0000FF", None, 2),
    ("00000000-0000-0000-0000-000000000003", "C", "Pass", "50.00", "64.99", "2.0", "#FFA500", None, 3),
    ("00000000-0000-0000-0000-000000000004", "F", "Fail", "0.00", "49.99", "0.0", "#FF0000", None, 4),
]

def assessment_insert(name, code, desc, cat, order):
    return (
        'INSERT INTO "AssessmentTypes" ("id", "Name", "Code", "Description", "Category", "IsActive", "IsSystemDefined", "SortOrder", "tenant_id", "created_at", "updated_at", "is_deleted")\n'
        "SELECT gen_random_uuid(), '" + name + "', '" + code + "', '" + desc + "', " + str(cat) + ", true, true, " + str(order) + ", " + TENANT_EXPR + ", now(), now(), false\n"
        "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = '" + code + "' AND x.\"tenant_id\" = " + TENANT_EXPR + ");\n"
    )

def band_insert(bid, letter, desc, lo, hi, gpa, color, hon, order):
    hon_sql = "NULL" if hon is None else "'" + hon + "'"
    return (
        'INSERT INTO "GradeBands" ("id", "GradingScaleId", "GradeLetter", "Description", "MinPercentage", "MaxPercentage", "GpaPoints", "ColorCode", "HonorsClassification", "SortOrder", "tenant_id", "created_at", "updated_at", "is_deleted")\n'
        "SELECT '" + bid + "', '00000000-0000-0000-0000-000000000001', '" + letter + "', '" + desc + "', " + lo + ", " + hi + ", " + gpa + ", '" + color + "', " + hon_sql + ", " + str(order) + ", " + TENANT_EXPR + ", now(), now(), false\n"
        "WHERE NOT EXISTS (SELECT 1 FROM \"GradeBands\" x WHERE x.\"id\" = '" + bid + "' AND x.\"tenant_id\" = " + TENANT_EXPR + ");\n"
    )

TYPES_SQL = SET_TENANT + "\n" + "\n".join(assessment_insert(*t) for t in TYPES)

SCALES_SQL = (
    SET_TENANT + "\n"
    'INSERT INTO "GradingScales" ("id", "Name", "Description", "Version", "IsActive", "IsDefault", "EffectiveFrom", "tenant_id", "created_at", "updated_at", "is_deleted")\n'
    "SELECT '00000000-0000-0000-0000-000000000001', 'Default Grading Scale', 'Standard 4-band grading scale', 1, true, true, now(), " + TENANT_EXPR + ", now(), now(), false\n"
    'WHERE NOT EXISTS (SELECT 1 FROM "GradingScales" x WHERE x."id" = \'00000000-0000-0000-0000-000000000001\' AND x."tenant_id" = ' + TENANT_EXPR + ");\n"
)

BANDS_SQL = "\n".join(band_insert(*b) for b in BANDS)

RULES_SQL = (
    SET_TENANT + "\n"
    'INSERT INTO "CertificateRules" ("id", "Name", "Description", "MinimumPassingPercentage", "MinimumPassingGradeLetter", "RequireAllMandatoryAssessments", "RequireNoOutstandingIncomplete", "RequireAllRequiredUnits", "IsActive", "IsVersioned", "Version", "EffectiveFrom", "tenant_id", "created_at", "updated_at", "is_deleted")\n'
    "SELECT '00000000-0000-0000-0000-000000000001', 'Default Certificate Rule', 'Default certificate eligibility rule', 50.00, 'F', true, true, true, true, true, 1, now(), " + TENANT_EXPR + ", now(), now(), false\n"
    'WHERE NOT EXISTS (SELECT 1 FROM "CertificateRules" x WHERE x."id" = \'00000000-0000-0000-0000-000000000001\' AND x."tenant_id" = ' + TENANT_EXPR + ");\n"
)

TYPES = [
    ("Assignment", "ASSIGNMENT", "Written assignments", 1, 1),
    ("Practical", "PRACTICAL", "Practical exercises", 2, 2),
    ("Laboratory", "LABORATORY", "Lab work", 3, 3),
    ("CAT", "CAT", "Continuous Assessment Test", 4, 4),
    ("Quiz", "QUIZ", "Online quizzes", 5, 5),
    ("Oral Examination", "ORAL", "Oral presentations and exams", 6, 6),
    ("Project", "PROJECT", "Major project work", 7, 7),
    ("Presentation", "PRESENTATION", "Class presentations", 8, 8),
    ("Final Examination", "FINALEXAM", "End-of-term final exam", 9, 9),
    ("Supplementary Examination", "SUPP", "Supplementary retake exam", 10, 10),
    ("Retake Examination", "RETAKE", "Full retake examination", 11, 11),
    ("Coursework", "COURSEWORK", "Overall coursework component", 12, 12),
    ("Participation", "PARTICIPATION", "Class participation and attendance", 13, 13),
]

BANDS = [
    ("00000000-0000-0000-0000-000000000001", "A", "Distinction", "75.00", "100.00", "4.0", "#00AA00", None, 1),
    ("00000000-0000-0000-0000-000000000002", "B", "Credit", "65.00", "74.99", "3.0", "#0000FF", None, 2),
    ("00000000-0000-0000-0000-000000000003", "C", "Pass", "50.00", "64.99", "2.0", "#FFA500", None, 3),
    ("00000000-0000-0000-0000-000000000004", "F", "Fail", "0.00", "49.99", "0.0", "#FF0000", None, 4),
]

def assessment_insert(name, code, desc, cat, order):
    return (
        "INSERT INTO \\\"AssessmentTypes\\\" (\\\"Id\\\", \\\"Name\\\", \\\"Code\\\", \\\"Description\\\", \\\"Category\\\", \\\"IsActive\\\", \\\"IsSystemDefined\\\", \\\"SortOrder\\\", \\\"tenant_id\\\", \\\"created_at\\\", \\\"updated_at\\\", \\\"is_deleted\\\")\n"
        "SELECT gen_random_uuid(), '" + name + "', '" + code + "', '" + desc + "', " + str(cat) + ", true, true, " + str(order) + ", " + TENANT_EXPR + ", now(), now(), false\n"
        "WHERE NOT EXISTS (SELECT 1 FROM \\\"AssessmentTypes\\\" x WHERE x.\\\"Code\\\" = '" + code + "' AND x.\\\"tenant_id\\\" = " + TENANT_EXPR + ");\n"
    )

def band_insert(bid, letter, desc, lo, hi, gpa, color, hon, order):
    hon_sql = "NULL" if hon is None else "'" + hon + "'"
    return (
        "INSERT INTO \\\"GradeBands\\\" (\\\"Id\\\", \\\"GradingScaleId\\\", \\\"GradeLetter\\\", \\\"Description\\\", \\\"MinPercentage\\\", \\\"MaxPercentage\\\", \\\"GpaPoints\\\", \\\"ColorCode\\\", \\\"HonorsClassification\\\", \\\"SortOrder\\\", \\\"tenant_id\\\", \\\"created_at\\\", \\\"updated_at\\\", \\\"is_deleted\\\")\n"
        "SELECT '" + bid + "', '00000000-0000-0000-0000-000000000001', '" + letter + "', '" + desc + "', " + lo + ", " + hi + ", " + gpa + ", '" + color + "', " + hon_sql + ", " + str(order) + ", " + TENANT_EXPR + ", now(), now(), false\n"
        "WHERE NOT EXISTS (SELECT 1 FROM \\\"GradeBands\\\" x WHERE x.\\\"Id\\\" = '" + bid + "' AND x.\\\"tenant_id\\\" = " + TENANT_EXPR + ");\n"
    )

TYPES_SQL = SET_TENANT + "\n" + "\n".join(assessment_insert(*t) for t in TYPES)

SCALES_SQL = (
    SET_TENANT + "\n"
    "INSERT INTO \\\"GradingScales\\\" (\\\"Id\\\", \\\"Name\\\", \\\"Description\\\", \\\"Version\\\", \\\"IsActive\\\", \\\"IsDefault\\\", \\\"EffectiveFrom\\\", \\\"tenant_id\\\", \\\"created_at\\\", \\\"updated_at\\\", \\\"is_deleted\\\")\n"
    "SELECT '00000000-0000-0000-0000-000000000001', 'Default Grading Scale', 'Standard 4-band grading scale', 1, true, true, now(), " + TENANT_EXPR + ", now(), now(), false\n"
    "WHERE NOT EXISTS (SELECT 1 FROM \\\"GradingScales\\\" x WHERE x.\\\"Id\\\" = '00000000-0000-0000-0000-000000000001' AND x.\\\"tenant_id\\\" = " + TENANT_EXPR + ");\n"
)

BANDS_SQL = "\n".join(band_insert(*b) for b in BANDS)

RULES_SQL = (
    SET_TENANT + "\n"
    "INSERT INTO \\\"CertificateRules\\\" (\\\"Id\\\", \\\"Name\\\", \\\"Description\\\", \\\"MinimumPassingPercentage\\\", \\\"MinimumPassingGradeLetter\\\", \\\"RequireAllMandatoryAssessments\\\", \\\"RequireNoOutstandingIncomplete\\\", \\\"RequireAllRequiredUnits\\\", \\\"IsActive\\\", \\\"IsVersioned\\\", \\\"Version\\\", \\\"EffectiveFrom\\\", \\\"tenant_id\\\", \\\"created_at\\\", \\\"updated_at\\\", \\\"is_deleted\\\")\n"
    "SELECT '00000000-0000-0000-0000-000000000001', 'Default Certificate Rule', 'Default certificate eligibility rule', 50.00, 'F', true, true, true, true, true, 1, now(), " + TENANT_EXPR + ", now(), now(), false\n"
    "WHERE NOT EXISTS (SELECT 1 FROM \\\"CertificateRules\\\" x WHERE x.\\\"Id\\\" = '00000000-0000-0000-0000-000000000001' AND x.\\\"tenant_id\\\" = " + TENANT_EXPR + ");\n"
)
