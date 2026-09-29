export interface Lane {
  id: string;
  laneName: string;
  description?: string;
  isActive: boolean;
  numberingFormat?: string;
  startingHouseNumber: number;
  totalHouses: number;
  occupiedHouses: number;
  vacantHouses: number;
  maintenanceCount: number;
  createdDate: string;
  updatedDate?: string;
}

export type OccupantType = 'Student' | 'Lecturer';

export interface House {
  id: string;
  laneId: string;
  laneName: string;
  houseNumber: string;
  houseName?: string;
  houseNumberNumeric: number;
  status: HouseStatusType;
  isOccupied: boolean;
  isEnabled: boolean;
  isAvailable: boolean;
  capacity: number;
  occupiedCount: number;
  remainingCapacity: number;
  occupantId?: string;
  occupantType?: OccupantType;
  occupantName?: string;
  studentNumber?: string;
  employeeNumber?: string;
  semesterId?: string;
  notes?: string;
  occupiedDate?: string;
  createdDate: string;
  updatedDate?: string;
}

export type HouseStatusType = 'Vacant' | 'Occupied' | 'Reserved' | 'Maintenance' | 'Disabled' | 'Unavailable';

export interface LaneOccupancy {
  laneId: string;
  laneName: string;
  totalHouses: number;
  occupied: number;
  vacant: number;
  reserved: number;
  maintenance: number;
  disabled: number;
  occupancyPercentage: number;
  totalCapacity: number;
  occupants: number;
  studentOccupants: number;
  lecturerOccupants: number;
  availableCapacity: number;
}

export interface AccommodationDashboard {
  totalLanes: number;
  totalHouses: number;
  occupiedHouses: number;
  vacantHouses: number;
  maintenanceCount: number;
  disabledCount: number;
  occupancyPercentage: number;
  totalCapacity: number;
  totalOccupants: number;
  studentOccupants: number;
  lecturerOccupants: number;
  availableCapacity: number;
  laneSummaries: LaneOccupancy[];
}

export interface CreateLaneRequest {
  laneName: string;
  description?: string;
  numberOfHouses: number;
  numberingFormat?: string;
  startingHouseNumber: number;
  defaultCapacity: number;
}

export interface UpdateLaneRequest {
  id?: string;
  laneName: string;
  description?: string;
  isActive: boolean;
}

export interface CreateHouseRequest {
  laneId: string;
  numberOfHouses: number;
  numberingFormat?: string;
  startingHouseNumber?: number;
  defaultCapacity: number;
}

export interface AssignHouseRequest {
  studentId?: string;
  lecturerId?: string;
  occupantType: OccupantType;
  houseId: string;
  semesterId?: string;
  moveInDate?: string;
  remarks?: string;
}

export interface CheckInRequest {
  checkInDate?: string;
  remarks?: string;
}

export interface CheckOutRequest {
  checkOutDate?: string;
  remarks?: string;
}

export interface AccommodationAssignment {
  id: string;
  studentId?: string;
  lecturerId?: string;
  occupantType: OccupantType;
  semesterId: string;
  assignmentDate: string;
  moveInDate?: string;
  moveOutDate?: string;
  checkInDate?: string;
  checkOutDate?: string;
  status: string;
  remarks?: string;
  studentName: string;
  studentNumber: string;
  lecturerName: string;
  employeeNumber: string;
  semesterName: string;
  houseId?: string;
  houseNumber: string;
  houseName?: string;
  laneId?: string;
  laneName: string;
  houseCapacity: number;
  houseOccupiedCount: number;
  isCheckedIn: boolean;
  isCheckedOut: boolean;
}

export interface ReassignHouseRequest {
  studentId?: string;
  lecturerId?: string;
  occupantType: OccupantType;
  newHouseId: string;
  remarks?: string;
}

export interface VacateHouseRequest {
  houseId?: string;
  vacatedDate?: string;
  remarks?: string;
}

export interface LaneOccupancyReport {
  laneId: string;
  laneName: string;
  totalHouses: number;
  occupied: number;
  vacant: number;
  reserved: number;
  maintenance: number;
  disabled: number;
  unavailable: number;
  occupancyPercentage: number;
  totalCapacity: number;
  occupants: number;
  availableCapacity: number;
  houses: House[];
}

export interface HouseOccupancyReport {
  houseId: string;
  houseNumber: string;
  houseName?: string;
  laneName: string;
  status: string;
  isOccupied: boolean;
  capacity: number;
  occupants: number;
  occupantName?: string;
  studentNumber?: string;
  employeeNumber?: string;
  occupiedDate?: string;
  vacatedDate?: string;
  notes?: string;
}

export interface StudentAccommodation {
  studentId: string;
  studentName: string;
  studentNumber: string;
  houseId?: string;
  houseNumber?: string;
  laneName?: string;
  assignmentStatus?: string;
  assignedDate?: string;
  moveInDate?: string;
  moveOutDate?: string;
  remarks?: string;
}

export interface LecturerAccommodation {
  lecturerId: string;
  lecturerName: string;
  employeeNumber: string;
  houseId?: string;
  houseNumber?: string;
  laneName?: string;
  assignmentStatus?: string;
  assignedDate?: string;
  moveInDate?: string;
  moveOutDate?: string;
  remarks?: string;
}

export interface VacantHouseReport {
  totalVacant: number;
  vacantHouses: House[];
}

export interface MaintenanceReport {
  totalUnderMaintenance: number;
  housesUnderMaintenance: House[];
}

export interface OccupancyStatistics {
  totalLanes: number;
  totalHouses: number;
  occupiedHouses: number;
  vacantHouses: number;
  reservedHouses: number;
  maintenanceHouses: number;
  disabledHouses: number;
  unavailableHouses: number;
  occupancyPercentage: number;
  totalCapacity: number;
  totalOccupants: number;
  studentOccupants: number;
  lecturerOccupants: number;
  availableCapacity: number;
  capacityUtilization: number;
  laneSummaries: LaneOccupancy[];
}

// ===== Accommodation Reports (occupancy, history, utilization, export) =====

export type AccommodationReportKey =
  | 'current-occupancy'
  | 'occupied-houses'
  | 'empty-houses'
  | 'occupancy-history'
  | 'house-history'
  | 'occupancy-by-period'
  | 'occupant-history'
  | 'utilization-summary';

export interface ReportFilterApplied {
  label: string;
  value: string;
}

export interface ReportPagination {
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AccommodationReportBase {
  reportKey: string;
  reportTitle: string;
  generatedAtUtc: string;
  generatedBy: string;
  appliedFilters: ReportFilterApplied[];
}

export interface OccupancySummaryReport {
  totalHouses: number;
  occupiedHouses: number;
  emptyHouses: number;
  totalCapacity: number;
  occupiedSpaces: number;
  availableSpaces: number;
  housesAtFullCapacity: number;
  housesWithAvailableCapacity: number;
  housesNeverOccupied: number;
  occupancyPercentage: number;
}

export interface HouseOccupantReport {
  assignmentId: string;
  occupantId: string;
  occupantType: OccupantType;
  occupantName: string;
  occupantNumber?: string | null;
  allocationDate: string;
  moveInDate?: string | null;
  checkInDate?: string | null;
  assignmentStatus: string;
  semesterId?: string | null;
  semesterName?: string | null;
}

export interface HouseOccupancyReportRow {
  houseId: string;
  houseNumber: string;
  houseName?: string | null;
  laneId: string;
  laneName: string;
  status: string;
  capacity: number;
  occupiedCount: number;
  availableSpaces: number;
  isOccupied: boolean;
  isAvailable: boolean;
  isEnabled: boolean;
  occupancyStatus: 'Occupied' | 'Empty' | 'Full' | string;
  currentOccupants: HouseOccupantReport[];
  lastOccupantName?: string | null;
  lastOccupantNumber?: string | null;
  lastOccupantType?: OccupantType | null;
  lastOccupancyEndDate?: string | null;
  historicalOccupantCount: number;
  occupiedDate?: string | null;
  vacatedDate?: string | null;
}

export interface AccommodationHouseOccupancyReport extends AccommodationReportBase {
  summary: OccupancySummaryReport;
  pagination: ReportPagination;
  rows: HouseOccupancyReportRow[];
}

export interface OccupancyHistoryReportRow {
  assignmentId: string;
  houseId: string;
  houseNumber: string;
  houseName?: string | null;
  laneName: string;
  occupantId: string;
  occupantType: OccupantType;
  occupantName: string;
  occupantNumber?: string | null;
  occupancyStartDate: string;
  occupancyEndDate?: string | null;
  isCurrent: boolean;
  durationDays: number;
  status: string;
  semesterId?: string | null;
  semesterName?: string | null;
  academicYearName?: string | null;
}

export interface OccupancyHistoryReport extends AccommodationReportBase {
  periodStart?: string | null;
  periodEnd?: string | null;
  distinctOccupants: number;
  distinctHouses: number;
  pagination: ReportPagination;
  rows: OccupancyHistoryReportRow[];
}

export interface HouseOccupancyHistoryReport extends AccommodationReportBase {
  houseId: string;
  houseNumber: string;
  houseName?: string | null;
  laneName: string;
  houseStatus: string;
  capacity: number;
  currentOccupants: number;
  totalStays: number;
  pagination: ReportPagination;
  rows: OccupancyHistoryReportRow[];
}

export interface OccupancyByPeriodHouseRow {
  houseId: string;
  houseNumber: string;
  houseName?: string | null;
  laneName: string;
  status: string;
  capacity: number;
  occupiedInPeriod: number;
  availableInPeriod: number;
  occupantsInPeriod: string;
  wasOccupiedInPeriod: boolean;
}

export interface OccupancyByPeriodReport extends AccommodationReportBase {
  periodStart?: string | null;
  periodEnd?: string | null;
  periodLabel: string;
  summary: OccupancySummaryReport;
  pagination: ReportPagination;
  rows: OccupancyByPeriodHouseRow[];
}

export interface OccupantCandidate {
  occupantId: string;
  occupantType: OccupantType;
  occupantName: string;
  occupantNumber?: string | null;
  currentHouseId?: string | null;
  currentHouseNumber?: string | null;
  totalStays: number;
  isCurrent: boolean;
}

export interface OccupantAccommodationHistoryReport extends AccommodationReportBase {
  mode: 'Search' | 'Detail' | string;
  selectedOccupant?: OccupantCandidate | null;
  candidates: OccupantCandidate[];
  stays: OccupancyHistoryReportRow[];
  currentHouse: string;
  pagination: ReportPagination;
}

export interface HouseUtilizationSummaryReport extends AccommodationReportBase {
  summary: OccupancySummaryReport;
}

export interface AccommodationReportParams {
  laneId?: string;
  houseId?: string;
  occupantId?: string;
  status?: string;
  occupantType?: OccupantType;
  semesterId?: string;
  academicYearId?: string;
  fromDate?: string;
  toDate?: string;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}

