import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { accommodationService } from './accommodation.service';
import { api } from './api';

/**
 * Contract tests for the accommodation report endpoints.
 *
 * These guard the frontend request shape against the routes actually declared
 * on AccommodationController. Both getLaneOccupancyReport and
 * getLecturerAccommodationList previously diverged from the backend: the former
 * sent laneId as a query string against a route-parameterised URL and typed the
 * response as a list, the latter sent a `status` filter the handler never
 * supported while being unable to send the `laneId` it does support.
 */
vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
    upload: vi.fn(),
  },
}));

const getMock = api.get as unknown as ReturnType<typeof vi.fn>;

interface RecordedCall {
  url: string;
  config?: { params?: Record<string, unknown> };
}

function lastCall(): RecordedCall {
  const call = getMock.mock.calls[getMock.mock.calls.length - 1];
  return { url: call[0] as string, config: call[1] as RecordedCall['config'] };
}

const LANE_ID = '9c0e5c4a-1d2f-4a5b-8c7d-6e5f4a3b2c1d';

describe('accommodationService report endpoints', () => {
  beforeEach(() => {
    getMock.mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('getLaneOccupancyReport', () => {
    it('puts the required lane id in the route, not in the query string', async () => {
      getMock.mockResolvedValue({ laneId: LANE_ID, laneName: 'Lane A' });

      await accommodationService.getLaneOccupancyReport(LANE_ID);

      const { url, config } = lastCall();
      expect(url).toBe(`/accommodation/reports/lane-occupancy/${LANE_ID}`);
      // A query-string laneId would not bind: the backend route is
      // reports/lane-occupancy/{laneId}, so the id must live in the path.
      expect(config).toBeUndefined();
    });

    it('returns the single report object the endpoint produces', async () => {
      const report = {
        laneId: LANE_ID,
        laneName: 'Lane A',
        totalHouses: 4,
        occupied: 1,
        vacant: 2,
        unavailable: 1,
        totalCapacity: 8,
        occupants: 3,
        occupancyPercentage: 37.5,
        houses: [],
      };
      getMock.mockResolvedValue(report);

      const result = await accommodationService.getLaneOccupancyReport(LANE_ID);

      // The endpoint returns one LaneOccupancyReportDto, never a collection.
      expect(result).toEqual(report);
      expect(Array.isArray(result)).toBe(false);
    });
  });

  describe('getLecturerAccommodationList', () => {
    it('sends only the filters the backend handler supports', async () => {
      getMock.mockResolvedValue([]);

      await accommodationService.getLecturerAccommodationList(LANE_ID, 'OKELLO');

      const { url, config } = lastCall();
      expect(url).toBe('/accommodation/reports/lecturer-accommodation');
      expect(config?.params).toEqual({ laneId: LANE_ID, searchTerm: 'OKELLO' });
      // GetLecturerAccommodationListQuery has no Status member; sending one
      // would be silently ignored and give a false impression of filtering.
      expect(config?.params).not.toHaveProperty('status');
    });

    it('sends no filters when none are supplied', async () => {
      getMock.mockResolvedValue([]);

      await accommodationService.getLecturerAccommodationList();

      const { url, config } = lastCall();
      expect(url).toBe('/accommodation/reports/lecturer-accommodation');
      expect(config?.params).toEqual({ laneId: undefined, searchTerm: undefined });
    });

    it('forwards the lecturer list returned by the endpoint', async () => {
      const rows = [{ houseId: 'h1', employeeNumber: 'EMP-1', fullName: 'DR J OKELLO' }];
      getMock.mockResolvedValue(rows);

      const result = await accommodationService.getLecturerAccommodationList();

      expect(result).toEqual(rows);
    });
  });

  describe('getStudentAccommodationList', () => {
    it('mirrors the lecturer report and drops the unsupported status filter', async () => {
      getMock.mockResolvedValue([]);

      await accommodationService.getStudentAccommodationList(LANE_ID, 'KWANZA');

      const { url, config } = lastCall();
      expect(url).toBe('/accommodation/reports/student-accommodation');
      expect(config?.params).toEqual({ laneId: LANE_ID, searchTerm: 'KWANZA' });
      expect(config?.params).not.toHaveProperty('status');
    });
  });

  describe('getHouseOccupancyReport', () => {
    it('keeps laneId and status, which the backend does accept', async () => {
      getMock.mockResolvedValue([]);

      await accommodationService.getHouseOccupancyReport(LANE_ID, 'Vacant');

      const { url, config } = lastCall();
      expect(url).toBe('/accommodation/reports/house-occupancy');
      // This endpoint genuinely takes both as query params, unlike the two above.
      expect(config?.params).toEqual({ laneId: LANE_ID, status: 'Vacant' });
    });
  });
});
