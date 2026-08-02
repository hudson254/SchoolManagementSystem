# Test Repair Progress Tracker

## Phase 1: Unit Test Infrastructure
- [x] Create TODO tracker
- [ ] Fix SMS.UnitTests.csproj - Add missing package references
- [ ] Fix BaseEntityTests.cs - Remove UpdateAudit, fix CreatedBy default

## Phase 2: Fix Individual Unit Test Files (8 files)
- [ ] Fix CreateStudentCommandTests.cs
- [ ] Fix UpdateStudentCommandTests.cs
- [ ] Fix RegisterCommandTests.cs
- [ ] Fix LoginCommandTests.cs
- [ ] Fix CreateCourseCommandTests.cs
- [ ] Fix CreateUnitCommandTests.cs
- [ ] Fix CreateAssignmentCommandTests.cs
- [ ] Fix AssignRoomCommandTests.cs

## Phase 3: Fix Integration Tests (2 files)
- [ ] Fix DatabaseFixture.cs
- [ ] Fix StudentRepositoryTests.cs

## Phase 4: Fix API Tests (4 files)
- [ ] Fix ApiTestFixture.cs
- [ ] Fix AuthControllerTests.cs
- [ ] Fix StudentControllerTests.cs
- [ ] Fix FullFlowTests.cs

## Phase 5: Build & Validation
- [ ] dotnet build - verify all compile
- [ ] dotnet test - verify all pass

## Progress
- Projects Building: 0/3
- Tests Passing: 0/12
- Tests Failing: 12/12

