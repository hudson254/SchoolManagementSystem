-- Seed data for Assessment & Grading System
-- Phase 3: Assessment Types
INSERT INTO AssessmentTypes (Id, Code, Name, Description, Category, SortOrder, IsActive, IsSystemDefined, CreatedDate)
VALUES
('00000000-0000-0000-0000-000000000001', 'ASSIGNMENT', 'Assignment', 'Written assignments', 0, 1, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000002', 'PRACTICAL', 'Practical', 'Practical exercises', 1, 2, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000003', 'LABORATORY', 'Laboratory', 'Lab work', 2, 3, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000004', 'CAT', 'CAT', 'Continuous Assessment Test', 3, 4, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000005', 'QUIZ', 'Quiz', 'Online quizzes', 4, 5, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000006', 'ORAL', 'Oral Examination', 'Oral presentations and exams', 5, 6, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000007', 'PROJECT', 'Project', 'Major project work', 6, 7, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000008', 'PRESENTATION', 'Presentation', 'Class presentations', 7, 8, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000009', 'FINALEXAM', 'Final Examination', 'End-of-term final exam', 8, 9, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000010', 'SUPP', 'Supplementary Examination', 'Supplementary retake exam', 9, 10, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000011', 'RETAKE', 'Retake Examination', 'Full retake examination', 10, 11, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000012', 'COURSEWORK', 'Coursework', 'Overall coursework component', 11, 12, 1, 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000013', 'PARTICIPATION', 'Participation', 'Class participation and attendance', 12, 13, 1, 1, GETUTCDATE());

-- Phase 8: Default Grading Scale
INSERT INTO GradingScales (Id, Name, Description, Version, IsDefault, IsActive, EffectiveFrom, CreatedDate)
VALUES ('00000000-0000-0000-0000-000000000001', 'Default Grading Scale', 'Standard 4-band grading scale', 1, 1, 1, GETUTCDATE(), GETUTCDATE());

INSERT INTO GradeBands (Id, GradingScaleId, GradeLetter, Description, MinPercentage, MaxPercentage, GpaPoints, ColorCode, SortOrder, CreatedDate)
VALUES
('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'A', 'Distinction', 75.00, 100.00, 4.0, '#00AA00', 1, GETUTCDATE()),
('00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'B', 'Credit', 65.00, 74.99, 3.0, '#0000FF', 2, GETUTCDATE()),
('00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'C', 'Pass', 50.00, 64.99, 2.0, '#FFA500', 3, GETUTCDATE()),
('00000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', 'F', 'Fail', 0.00, 49.99, 0.0, '#FF0000', 4, GETUTCDATE());

-- Phase 9: Default Certificate Rules
INSERT INTO CertificateRules (Id, Name, MinimumPassingGrade, MinimumOverallPercentage, RequireAllMandatoryAssessments, NoOutstandingIncompletes, RequireAllUnitsPassed, IsActive, EffectiveFrom, CreatedDate)
VALUES ('00000000-0000-0000-0000-000000000001', 'Default Certificate Rule', 'F', 50.00, 1, 1, 1, 1, GETUTCDATE(), GETUTCDATE());
