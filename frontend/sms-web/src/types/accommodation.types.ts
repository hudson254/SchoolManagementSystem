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

export interface House {
  id: string;
  laneId: string;
  laneName: string;
  houseNumber: string;
  houseNumberNumeric: number;
  status: HouseStatusType;
  isOccupied: boolean;
  isEnabled: boolean;
  isAvailable: boolean;
  occupantId?: string;
  occupantName?: string;
  studentNumber?: string;
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
}

export interface AccommodationDashboard {
  totalLanes: number;
  totalHouses: number;
  occupiedHouses: number;
  vacantHouses: number;
  maintenanceCount: number;
  disabledCount: number;
  occupancyPercentage: number;
  laneSummaries: LaneOccupancy[];
}

export interface CreateLaneRequest {
  laneName: string;
  description?: string;
  numberOfHouses: number;
  numberingFormat?: string;
  startingHouseNumber: number;
}

export interface UpdateLaneRequest {
  id: string;
  laneName: string;
  description?: string;
  isActive: boolean;
}

export interface CreateHouseRequest {
  laneId: string;
  numberOfHouses: number;
  numberingFormat?: string;
  startingHouseNumber?: number;
}

export interface AssignHouseRequest {
  studentId: string;
  houseId: string;
  semesterId: string;
  moveInDate?: string;
  remarks?: string;
}

export interface ReassignHouseRequest {
  studentId: string;
  newHouseId: string;
  remarks?: string;
}

export interface VacateHouseRequest {
  houseId: string;
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
  houses: House[];
}

export interface HouseOccupancyReport {
  houseId: string;
  houseNumber: string;
  laneName: string;
  status: string;
  isOccupied: boolean;
  occupantName?: string;
  studentNumber?: string;
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
  laneSummaries: LaneOccupancy[];
}

