import { api } from './api';

interface Building {
  id: string;
  name: string;
  address?: string;
  totalFloors: number;
  hasElevator: boolean;
  category?: string;
  isActive: boolean;
  totalBlocks: number;
  totalRooms: number;
}

interface BuildingDetails extends Building {
  blocks: Block[];
  occupiedRooms: number;
  availableRooms: number;
  occupancyRate: number;
}

interface Block {
  id: string;
  name: string;
  buildingId: string;
  floorNumber: number;
  totalRooms: number;
  category?: string;
  isActive: boolean;
  occupiedRooms: number;
  availableRooms: number;
}

interface Room {
  id: string;
  roomNumber: string;
  blockId: string;
  capacity: number;
  roomType?: string;
  pricePerSemester: number;
  facilities?: string;
  isAvailable: boolean;
  isOccupied: boolean;
  status: string;
  blockName: string;
  buildingName: string;
  currentOccupant?: string;
}

interface AccommodationAssignment {
  id: string;
  studentId: string;
  roomId: string;
  semesterId: string;
  assignmentDate: string;
  moveInDate?: string;
  moveOutDate?: string;
  status: string;
  assignedBy?: string;
  remarks?: string;
  studentName: string;
  studentNumber: string;
  roomNumber: string;
  blockName: string;
  buildingName: string;
  semesterName: string;
}

interface OccupancyReport {
  totalRooms: number;
  occupiedRooms: number;
  availableRooms: number;
  maintenanceRooms: number;
  occupancyRate: number;
  buildingOccupancy: BuildingOccupancy[];
}

interface BuildingOccupancy {
  buildingName: string;
  totalRooms: number;
  occupiedRooms: number;
  availableRooms: number;
  occupancyRate: number;
}

interface GetRoomsParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  buildingId?: string;
  blockId?: string;
  isAvailable?: boolean;
  isOccupied?: boolean;
  roomType?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const accommodationService = {
  getBuildings: () =>
    api.get<Building[]>('/accommodation/buildings'),

  getBuilding: (id: string) =>
    api.get<BuildingDetails>(`/accommodation/buildings/${id}`),

  createBuilding: (data: any) =>
    api.post<Building>('/accommodation/buildings', data),

  getRooms: (params: GetRoomsParams) =>
    api.get<PagedResponse<Room>>('/accommodation/rooms', { params }),

  getAvailableRooms: (buildingId?: string, blockId?: string) =>
    api.get<Room[]>('/accommodation/rooms/available', { params: { buildingId, blockId } }),

  assignRoom: (data: { roomId: string; studentId: string; semesterId: string; remarks?: string }) =>
    api.post<AccommodationAssignment>(`/accommodation/rooms/${data.roomId}/assign`, data),

  transferRoom: (assignmentId: string, newRoomId: string, remarks?: string) =>
    api.post<AccommodationAssignment>(`/accommodation/assignments/${assignmentId}/transfer`, { newRoomId, remarks }),

  vacateRoom: (assignmentId: string, remarks?: string) =>
    api.post(`/accommodation/assignments/${assignmentId}/vacate`, { remarks }),

  getOccupancyReport: (buildingId?: string) =>
    api.get<OccupancyReport>('/accommodation/reports/occupancy', { params: { buildingId } }),

  getStudentAssignment: (studentId: string, semesterId?: string) =>
    api.get<AccommodationAssignment>(`/accommodation/assignments/student/${studentId}`, { params: { semesterId } }),
};