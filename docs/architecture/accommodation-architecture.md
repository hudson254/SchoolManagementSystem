# Accommodation Module Architecture

## Overview

The Accommodation module has been redesigned from a legacy Building/Room model to a modern Lane/House model that supports housing estate management. The new architecture allows administrators to define housing layouts by creating lanes and assigning configurable numbers of houses to each lane.

---

## Architecture Principles

- **Clean Architecture**: Domain entities, application logic, persistence, and presentation layers are strictly separated.
- **CQRS**: All operations use separate Command and Query handlers via MediatR.
- **SOLID Principles**: Single responsibility, open/closed, Liskov substitution, interface segregation, dependency inversion.
- **Multi-tenancy**: All entities implement `ITenantAwareEntity` with global query filters.
- **RBAC**: All endpoints are protected using authorization policies.
- **Audit Trail**: All CRUD operations are recorded via `IAuditService`.

---

## Accommodation Structure

```
Accommodation Area
    ├── Lane (e.g., Lane A, East Lane, Staff Lane)
    │      ├── House 001
    │      ├── House 002
    │      ├── House 003
    │      └── ...
    ├── Lane (e.g., Lane B, North Lane)
    │      ├── House 001
    │      ├── House 002
    │      └── ...
```

Each house represents one residential unit occupied by one student.

---

## Domain Entities

### Lane
- **LaneId** (Guid, inherited from BaseEntity.Id)
- **LaneName** (string, max 100, required, unique per tenant)
- **Description** (string, max 500, optional)
- **IsActive** (bool, default true)
- **NumberingFormat** (string, max 20, default "D3" for 001, 002...)
- **StartingHouseNumber** (int, default 1)
- **Houses** (ICollection<House>, navigation property)
- TenantId, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy, IsDeleted (inherited from BaseEntity + ITenantAwareEntity)

### House
- **HouseId** (Guid, inherited from BaseEntity.Id)
- **LaneId** (Guid, required, FK to Lane)
- **HouseNumber** (string, max 20, required, unique per lane)
- **HouseNumberNumeric** (int, for sorting purposes)
- **Status** (string, max 30). Valid values: Vacant, Occupied, Reserved, Maintenance, Disabled, Unavailable
- **IsOccupied** (bool)
- **OccupantId** (Guid?, FK to Student)
- **IsEnabled** (bool, default true)
- **IsAvailable** (bool, default true)
- **SemesterId** (Guid?, FK to Semester)
- **Notes** (string, max 500)
- **OccupiedDate** (DateTime?)
- **VacatedDate** (DateTime?)
- Navigation: Lane, Occupant (Student), Semester, Accommodations, AccommodationAssignments

### Accommodation (Legacy Backward Compatibility)
- **AccommodationId** (Guid)
- **StudentId** (Guid, FK to Student)
- **HouseId** (Guid, FK to House)
- **LaneId** (Guid, FK to Lane)
- **RoomId** (Guid?, legacy, nullable)
- Status, AssignedDate, VacatedDate, IsActive

### AccommodationAssignment
- **AssignmentId** (Guid)
- **StudentId** (Guid, FK to Student, unique)
- **HouseId** (Guid, FK to House)
- **LaneId** (Guid, FK to Lane)
- **RoomId** (Guid?, legacy, nullable)
- **SemesterId** (Guid, FK to Semester)
- Status (Active, Vacated, Completed)
- AssignmentDate, AssignedDate, MoveInDate, MoveOutDate, Remarks

---

## API Endpoints

### Lane Management
| Method | Endpoint | Description | Auth Policy |
|--------|----------|-------------|-------------|
| GET | /api/v1/accommodation/lanes | List lanes (with search) | ReceptionistAccess |
| GET | /api/v1/accommodation/lanes/{id} | Get lane details | ReceptionistAccess |
| POST | /api/v1/accommodation/lanes | Create lane with auto house generation | AdministratorAccess |
| PUT | /api/v1/accommodation/lanes/{id} | Update lane | AdministratorAccess |
| DELETE | /api/v1/accommodation/lanes/{id} | Delete lane (only if empty) | AdministratorAccess |

### House Management
| Method | Endpoint | Description | Auth Policy |
|--------|----------|-------------|-------------|
| GET | /api/v1/accommodation/houses | List houses (with filters) | ReceptionistAccess |
| GET | /api/v1/accommodation/houses/{id} | Get house details | ReceptionistAccess |
| GET | /api/v1/accommodation/lanes/{laneId}/houses | List houses in lane | ReceptionistAccess |
| POST | /api/v1/accommodation/houses | Create houses manually | AdministratorAccess |
| PUT | /api/v1/accommodation/houses/{id} | Update house | AdministratorAccess |
| DELETE | /api/v1/accommodation/houses/{id} | Delete house (if vacant) | AdministratorAccess |
| GET | /api/v1/accommodation/houses/available | List available houses | ReceptionistAccess |

### House Status Management
| Method | Endpoint | Description | Auth Policy |
|--------|----------|-------------|-------------|
| POST | /api/v1/accommodation/houses/{houseId}/maintenance | Toggle maintenance status | AdministratorAccess |
| POST | /api/v1/accommodation/houses/{houseId}/unavailable | Toggle unavailable status | AdministratorAccess |

### Allocation
| Method | Endpoint | Description | Auth Policy |
|--------|----------|-------------|-------------|
| POST | /api/v1/accommodation/houses/{houseId}/assign | Assign student to house | ReceptionistAccess |
| POST | /api/v1/accommodation/houses/{houseId}/reassign | Reassign student | ReceptionistAccess |
| POST | /api/v1/accommodation/houses/{houseId}/vacate | Vacate house | ReceptionistAccess |

### Dashboard and Reports
| Method | Endpoint | Description | Auth Policy |
|--------|----------|-------------|-------------|
| GET | /api/v1/accommodation/dashboard | Dashboard stats | ReceptionistAccess |
| GET | /api/v1/accommodation/reports/lane-occupancy/{laneId} | Lane occupancy report | ReceptionistAccess |
| GET | /api/v1/accommodation/reports/house-occupancy | House occupancy report | ReceptionistAccess |
| GET | /api/v1/accommodation/reports/student-accommodation | Student accommodation list | ReceptionistAccess |
| GET | /api/v1/accommodation/reports/vacant-houses | Vacant houses report | ReceptionistAccess |
| GET | /api/v1/accommodation/reports/maintenance | Maintenance report | ReceptionistAccess |
| GET | /api/v1/accommodation/reports/statistics | Occupancy statistics | ReceptionistAccess |

---

## CQRS Handlers

### Commands
1. **CreateLaneCommand** - Creates lane + auto-generates houses
2. **UpdateLaneCommand** - Updates lane name/description/status
3. **DeleteLaneCommand** - Soft-deletes lane (validates no houses exist)
4. **CreateHouseCommand** - Manually adds houses to existing lane
5. **UpdateHouseCommand** - Updates house number/status/notes
6. **DeleteHouseCommand** - Soft-deletes house (validates no occupant)
7. **SetHouseMaintenanceCommand** - Toggles maintenance status
8. **SetHouseUnavailableCommand** - Toggles unavailable status
9. **AssignHouseCommand** - Assigns student to house (prevents double allocation)
10. **VacateHouseCommand** - Vacates house
11. **ReassignHouseCommand** - Reassigns student to different house

### Queries
1. **GetLanesQuery** - All lanes with occupancy stats (searchable)
2. **GetLaneQuery** - Single lane with occupancy stats
3. **GetHousesQuery** - Houses filtered by lane/status/search
4. **GetHouseQuery** - Single house with occupant details
5. **GetAvailableHousesQuery** - Available (vacant, enabled) houses
6. **GetAccommodationDashboardQuery** - Dashboard with summaries

### Report Queries
1. **GetLaneOccupancyReportQuery** - Detailed lane occupancy report
2. **GetHouseOccupancyReportQuery** - House-level occupancy details
3. **GetStudentAccommodationListQuery** - Student accommodation list
4. **GetVacantHouseReportQuery** - Vacant houses report
5. **GetMaintenanceReportQuery** - Houses under maintenance
6. **GetOccupancyStatisticsQuery** - Overall occupancy statistics

---

## Validation Rules

- Lane names must be unique within a tenant.
- House numbers must be unique within each lane.
- Occupied houses cannot be allocated twice.
- Deleted lanes cannot contain houses (soft-delete blocked if houses exist).
- Deleted houses cannot have active occupants (soft-delete blocked if occupied).
- All operations respect tenant isolation via global query filters.
- House status transitions are validated (e.g., occupied houses cannot be set to maintenance).

---

## RBAC Permissions

```csharp
public static class AccommodationPermissions
{
    public const string View = "Accommodation.View";
    public const string Create = "Accommodation.Create";
    public const string Edit = "Accommodation.Edit";
    public const string Delete = "Accommodation.Delete";
    public const string Assign = "Accommodation.Assign";
    public const string Reassign = "Accommodation.Reassign";
    public const string Reports = "Accommodation.Reports";
    public const string Maintenance = "Accommodation.Maintenance";
    public const string Vacate = "Accommodation.Vacate";
}
```

---

## Audit Trail

All accommodation operations are audited via `IAuditService.LogAsync()`:

- Lane creation, modification, deletion
- House creation, modification, deletion
- Student assignment, reassignment
- House vacancy
- Maintenance status changes

Audit records include: User, Timestamp, Previous Values, New Values, Tenant, Correlation ID, IP Address.

---

## Database Schema

### Table: Lanes

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier | PK |
| LaneName | nvarchar(100) | NOT NULL |
| Description | nvarchar(500) | NULL |
| IsActive | bit | NOT NULL (default 1) |
| NumberingFormat | nvarchar(20) | NOT NULL (default 'D3') |
| StartingHouseNumber | int | NOT NULL (default 1) |
| TenantId | uniqueidentifier | NOT NULL |
| CreatedDate | datetime2 | NOT NULL |
| CreatedBy | nvarchar(100) | NULL |
| ModifiedDate | datetime2 | NULL |
| ModifiedBy | nvarchar(100) | NULL |
| IsDeleted | bit | NOT NULL (default 0) |
| DeletedDate | datetime2 | NULL |
| DeletedBy | nvarchar(100) | NULL |

**Indexes**: 
- UNIQUE (LaneName, TenantId) WHERE IsDeleted = 0

### Table: Houses

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier | PK |
| LaneId | uniqueidentifier | FK → Lanes(Id) NOT NULL |
| HouseNumber | nvarchar(20) | NOT NULL |
| HouseNumberNumeric | int | NOT NULL |
| Status | nvarchar(30) | NOT NULL |
| IsOccupied | bit | NOT NULL |
| OccupantId | uniqueidentifier | FK → Students(Id), NULL |
| IsEnabled | bit | NOT NULL (default 1) |
| IsAvailable | bit | NOT NULL (default 1) |
| SemesterId | uniqueidentifier | FK → Semesters(Id), NULL |
| Notes | nvarchar(500) | NULL |
| OccupiedDate | datetime2 | NULL |
| VacatedDate | datetime2 | NULL |
| TenantId | uniqueidentifier | NOT NULL |
| CreatedDate | datetime2 | NOT NULL |
| CreatedBy | nvarchar(100) | NULL |
| ModifiedDate | datetime2 | NULL |
| ModifiedBy | nvarchar(100) | NULL |
| IsDeleted | bit | NOT NULL (default 0) |
| DeletedDate | datetime2 | NULL |
| DeletedBy | nvarchar(100) | NULL |

**Indexes**:
- UNIQUE (LaneId, HouseNumber) WHERE IsDeleted = 0
- FK: LaneId → Lanes(Id) ON DELETE CASCADE
- FK: OccupantId → Students(Id) ON DELETE SET NULL
- FK: SemesterId → Semesters(Id) ON DELETE RESTRICT

---

## Auto House Generation

When creating a lane, administrators specify:
- Lane name
- Number of houses
- Numbering format (e.g., D3 for 001, D4 for 0001)
- Starting house number (default: 1)

The system automatically generates houses using:
```csharp
var format = request.NumberingFormat ?? "D3";
for (int i = 0; i < request.NumberOfHouses; i++)
{
    var houseNumber = request.StartingHouseNumber + i;
    houses.Add(new House
    {
        HouseNumber = houseNumber.ToString(format),
        HouseNumberNumeric = houseNumber,
        Status = HouseStatus.Vacant,
        IsOccupied = false,
        IsEnabled = true,
        IsAvailable = true
    });
}
```

---

## Occupancy Tracking

The system tracks:
- **Occupied**: Houses with an assigned student
- **Vacant**: Empty houses ready for assignment
- **Reserved**: Pre-allocated houses
- **Maintenance**: Houses under maintenance
- **Disabled**: Permanently disabled houses
- **Unavailable**: Temporarily unavailable houses

Occupancy summary methods:
```csharp
// Per lane
Task<(int Total, int Occupied, int Vacant, int Maintenance, int Disabled, int Reserved)> 
    GetLaneOccupancySummaryAsync(Guid laneId);

// Overall 
Task<(int Total, int Occupied, int Vacant, int Maintenance, int Disabled)> 
    GetOverallOccupancySummaryAsync();
```

