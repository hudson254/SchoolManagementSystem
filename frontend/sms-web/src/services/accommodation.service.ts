import { api } from './api';
import type {
  Lane,
  House,
  AccommodationDashboard,
  CreateLaneRequest,
  UpdateLaneRequest,
  CreateHouseRequest,
  AssignHouseRequest,
  ReassignHouseRequest,
  VacateHouseRequest,
  AccommodationAssignment,
  CheckInRequest,
  CheckOutRequest,
  LaneOccupancyReport,
  HouseOccupancyReport,
  StudentAccommodation,
  LecturerAccommodation,
  VacantHouseReport,
  MaintenanceReport,
  OccupancyStatistics,
} from '../types/accommodation.types';

export const accommodationService = {
  // ===== Lane Management =====
  getLanes: (searchTerm?: string) =>
    api.get<Lane[]>('/accommodation/lanes', { params: { searchTerm } }),

  getLane: (id: string) =>
    api.get<Lane>(`/accommodation/lanes/${id}`),

  createLane: (data: CreateLaneRequest) =>
    api.post<Guid>('/accommodation/lanes', data),

  updateLane: (id: string, data: UpdateLaneRequest) =>
    api.put<boolean>(`/accommodation/lanes/${id}`, data),

  deleteLane: (id: string) =>
    api.delete(`/accommodation/lanes/${id}`),

  // ===== House Management =====
  getHouses: (laneId?: string, searchTerm?: string, status?: string) =>
    api.get<House[]>('/accommodation/houses', { params: { laneId, searchTerm, status } }),

  getLaneHouses: (laneId: string) =>
    api.get<House[]>(`/accommodation/lanes/${laneId}/houses`),

  getHouse: (id: string) =>
    api.get<House>(`/accommodation/houses/${id}`),

  createHouses: (data: CreateHouseRequest) =>
    api.post<string[]>('/accommodation/houses', data),

  updateHouse: (id: string, data: {
    houseNumber?: string;
    houseName?: string;
    status?: string;
    isEnabled?: boolean;
    isAvailable?: boolean;
    capacity?: number;
    notes?: string;
  }) => api.put<boolean>(`/accommodation/houses/${id}`, data),

  deleteHouse: (id: string) =>
    api.delete(`/accommodation/houses/${id}`),

  setHouseMaintenance: (houseId: string, isUnderMaintenance: boolean, notes?: string) =>
    api.post<boolean>(`/accommodation/houses/${houseId}/maintenance`, { isUnderMaintenance, notes }),

  setHouseUnavailable: (houseId: string, isUnavailable: boolean, notes?: string) =>
    api.post<boolean>(`/accommodation/houses/${houseId}/unavailable`, { isUnavailable, notes }),

  // ===== Allocation =====
  assignHouse: (houseId: string, data: AssignHouseRequest) =>
    api.post<Guid>(`/accommodation/houses/${houseId}/assign`, data),

  reassignHouse: (occupantId: string, data: ReassignHouseRequest) =>
    api.post<boolean>(`/accommodation/houses/${data.newHouseId}/reassign`, {
      studentId: data.occupantType === 'Student' ? occupantId : undefined,
      lecturerId: data.occupantType === 'Lecturer' ? occupantId : undefined,
      occupantType: data.occupantType,
      newHouseId: data.newHouseId,
      remarks: data.remarks,
    }),

  vacateHouse: (houseId: string, data?: VacateHouseRequest) =>
    api.post(`/accommodation/houses/${houseId}/vacate`, data ?? {}),

  getAvailableHouses: (laneId?: string) =>
    api.get<House[]>('/accommodation/houses/available', { params: { laneId } }),

  // ===== Assignments / Occupancy =====
  getAssignments: (params?: { laneId?: string; houseId?: string; searchTerm?: string }) =>
    api.get<AccommodationAssignment[]>('/accommodation/assignments', { params }),

  checkInAssignment: (assignmentId: string, data?: CheckInRequest) =>
    api.post<boolean>(`/accommodation/assignments/${assignmentId}/check-in`, data ?? {}),

  checkOutAssignment: (assignmentId: string, data?: CheckOutRequest) =>
    api.post<boolean>(`/accommodation/assignments/${assignmentId}/check-out`, data ?? {}),

  // ===== Dashboard =====
  getDashboard: () =>
    api.get<AccommodationDashboard>('/accommodation/dashboard'),

  // ===== Reports =====
  getLaneOccupancyReport: (laneId?: string) =>
    api.get<LaneOccupancyReport[]>('/accommodation/reports/lane-occupancy', { params: { laneId } }),

  getHouseOccupancyReport: (laneId?: string, status?: string) =>
    api.get<HouseOccupancyReport[]>('/accommodation/reports/house-occupancy', { params: { laneId, status } }),

  getStudentAccommodationList: (searchTerm?: string, status?: string) =>
    api.get<StudentAccommodation[]>('/accommodation/reports/student-accommodation', { params: { searchTerm, status } }),

  getLecturerAccommodationList: (searchTerm?: string, status?: string) =>
    api.get<LecturerAccommodation[]>('/accommodation/reports/lecturer-accommodation', { params: { searchTerm, status } }),

  getVacantHouseReport: (laneId?: string) =>
    api.get<VacantHouseReport>('/accommodation/reports/vacant-houses', { params: { laneId } }),

  getMaintenanceReport: () =>
    api.get<MaintenanceReport>('/accommodation/reports/maintenance'),

  getOccupancyStatistics: () =>
    api.get<OccupancyStatistics>('/accommodation/reports/occupancy-statistics'),

  // ===== Legacy (kept for backward compatibility) =====
  getBuildings: () =>
    api.get<any[]>('/accommodation/buildings'),

  getBuilding: (id: string) =>
    api.get<any>(`/accommodation/buildings/${id}`),

  createBuilding: (data: any) =>
    api.post<any>('/accommodation/buildings', data),

  getRooms: (params: any) =>
    api.get<any>('/accommodation/rooms', { params }),

  getAvailableRooms: (buildingId?: string, blockId?: string) =>
    api.get<any[]>('/accommodation/rooms/available', { params: { buildingId, blockId } }),

  assignRoom: (data: { roomId: string; studentId: string; semesterId: string; remarks?: string }) =>
    api.post<any>(`/accommodation/rooms/${data.roomId}/assign`, data),

  transferRoom: (assignmentId: string, newRoomId: string, remarks?: string) =>
    api.post<any>(`/accommodation/assignments/${assignmentId}/transfer`, { newRoomId, remarks }),

  vacateRoom: (assignmentId: string, remarks?: string) =>
    api.post(`/accommodation/assignments/${assignmentId}/vacate`, { remarks }),

  getOccupancyReport: (buildingId?: string) =>
    api.get<any>('/accommodation/reports/occupancy', { params: { buildingId } }),

  getVacantRoomsReport: (buildingId?: string) =>
    api.get<any>('/accommodation/reports/vacant', { params: { buildingId } }),

  getStudentAssignment: (studentId: string) =>
    api.get<any>(`/accommodation/assignments/student/${studentId}`),

  getLecturerAssignment: (lecturerId: string) =>
    api.get<any>(`/accommodation/assignments/lecturer/${lecturerId}`),
};

type Guid = string;

