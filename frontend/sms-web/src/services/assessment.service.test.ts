import { describe, it, expect, vi, beforeEach } from 'vitest';

const apiMock = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
}));

vi.mock('../services/api', () => ({
  api: apiMock,
}));

import { assessmentService } from '../services/assessment.service';

describe('assessmentService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('fetches assessment types from the backend endpoint', async () => {
    apiMock.get.mockResolvedValue([]);
    await assessmentService.getTypes();
    expect(apiMock.get).toHaveBeenCalledWith('/assessment/types');
  });

  it('fetches assessments for a unit', async () => {
    apiMock.get.mockResolvedValue([]);
    await assessmentService.getAssessmentsByUnit('unit-1');
    expect(apiMock.get).toHaveBeenCalledWith('/assessment/unit/unit-1');
  });

  it('fetches weight validation for a unit', async () => {
    apiMock.get.mockResolvedValue({ isValid: true, totalWeight: 100, weights: [], errors: [] });
    const result = await assessmentService.getWeightValidation('unit-1');
    expect(apiMock.get).toHaveBeenCalledWith('/assessment/unit/unit-1/weights');
    expect(result.isValid).toBe(true);
    expect(result.totalWeight).toBe(100);
  });

  it('submits a unit for review', async () => {
    apiMock.post.mockResolvedValue({});
    await assessmentService.submitForReview('unit-1', 'offering-1', 'ready');
    expect(apiMock.post).toHaveBeenCalledWith('/assessment/results/submit', {
      unitId: 'unit-1',
      courseOfferingId: 'offering-1',
      comments: 'ready',
    });
  });

  it('fetches published results for a student', async () => {
    apiMock.get.mockResolvedValue([]);
    await assessmentService.getStudentResults('student-1');
    expect(apiMock.get).toHaveBeenCalledWith('/assessment/student/student-1/results');
  });

  it('imports marks through the bulk endpoint', async () => {
    apiMock.post.mockResolvedValue({ totalRecords: 1, successCount: 1, errorCount: 0, errors: [] });
    await assessmentService.importMarks({
      assessmentId: 'assessment-1',
      records: [{ studentId: 'student-1', score: 85, maxScore: 100 }],
    });
    expect(apiMock.post).toHaveBeenCalledWith('/assessment/marks/import', {
      assessmentId: 'assessment-1',
      records: [{ studentId: 'student-1', score: 85, maxScore: 100 }],
    });
  });
});