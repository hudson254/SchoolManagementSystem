# Accommodation Module: Student & Lecturer Support - Implementation TODO

## Phase 1: Domain Layer
- [x] Create `OccupantType` enum (`src/SMS.Domain/Enums/OccupantType.cs`)
- [x] Update `AccommodationAssignment.cs` - Add `LecturerId`, `OccupantType`, `Lecturer` navigation
- [x] Update `Accommodation.cs` - Add `LecturerId`, `OccupantType`, `Lecturer` navigation
- [x] Update `House.cs` - Add `OccupantType`, `Lecturer` navigation
- [x] Update `Lecturer.cs` - Add accommodation navigation properties
- [x] Update `Student.cs` - Ensure backward compatibility

## Phase 2: Persistence Layer
- [x] Update `ApplicationDbContext.cs` - Configure Lecturer relationships for Accommodation, AccommodationAssignment, House

## Phase 3: Repository Layer
- [x] Update `IAccommodationRepository.cs` - Add lecturer-based methods
- [x] Update `AccommodationRepository.cs` - Implement new lecturer methods

## Phase 4: Application Layer - DTOs
- [x] Update `AccommodationDto.cs` - Add Lecturer fields to AccommodationAssignmentDto
- [x] Update `LaneDto.cs` - Add OccupantType, EmployeeNumber; create OccupantAccommodationDto

## Phase 5: Application Layer - Commands
- [x] Update `AssignHouseCommand.cs` - Add LecturerId, OccupantType
- [x] Update `ReassignHouseCommand.cs` - Add LecturerId, OccupantType
- [x] Update `VacateHouseCommand.cs` - Support both occupant types
- [ ] Update `AssignRoomCommand.cs` / `TransferRoomCommand.cs` - Support both types

## Phase 6: Application Layer - Queries
- [x] Update `GetStudentAssignmentQuery.cs` - Support both types
- [x] Update `GetStudentAccommodationListQuery.cs` - Support both types
- [x] Create `GetLecturerAssignmentQuery.cs`
- [x] Create `GetLecturerAccommodationListQuery.cs`

## Phase 7: API Layer
- [x] Update `AccommodationController.cs` - Add lecturer endpoints

## Phase 8: Frontend
- [x] Update `accommodation.types.ts` - Add OccupantType, LecturerId, EmployeeNumber
- [x] Update `accommodation.service.ts` - Add lecturer endpoints

## Phase 9: Tests
- [x] Update existing accommodation tests for lecturer scenarios
- [x] Create `LecturerAccommodationTests.cs`

## Phase 10: Build & Verify
- [x] Build the entire solution
- [x] Run all automated tests
- [x] Fix any compilation errors or test failures
- [x] Thorough testing: Unit tests (141 passed), API tests (62 passed), Integration tests (29 passed), Frontend tests (38 passed)
