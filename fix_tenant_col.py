# -*- coding: utf-8 -*-
import io

p = "src/SMS.Persistence/Migrations/20260908120000_SeedAssessmentGradingData.cs"
with io.open(p, "r", encoding="utf-8") as fh:
    t = fh.read()

# Replace the bare PK column reference (escaped double-quote + Id + escaped double-quote)
# with the actual lowercase column name "id".  The pattern \"Id\" (as it appears in the
# C# string) must NOT match GradingScaleId / AssessmentTypeId because those are preceded
# by other characters (no standalone quote before 'Id' there).
old = '\\"Id\\"'
new = '\\"id\\"'
count = t.count(old)
t = t.replace(old, new)

with io.open(p, "w", encoding="utf-8", newline="") as fh:
    fh.write(t)

print("REPLACED", count, "occurrences of escaped \"Id\" -> \"id\"")
# sanity: ensure we did not corrupt the FK column name
print("GradingScaleId refs intact:", t.count('GradingScaleId\\"'))