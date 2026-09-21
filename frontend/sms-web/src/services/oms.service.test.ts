import { describe, it, expect, vi, beforeEach } from 'vitest';

const apiMock = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  patch: vi.fn(),
  delete: vi.fn(),
}));

vi.mock('../services/api', () => ({
  api: apiMock,
}));

import { omsService, normalizeOmsPagedResult } from '../services/oms.service';

const envelope = {
  items: [{ id: 'order-1', orderNumber: 'ORD-2026-000001', statusName: 'Draft' }],
  totalCount: 21,
  pageNumber: 2,
  pageSize: 10,
  totalPages: 3,
  hasPreviousPage: true,
  hasNextPage: true,
};

describe('omsService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('requests the dashboard summary from the OMS dashboard endpoint', async () => {
    apiMock.get.mockResolvedValue({ draftCount: 1, totalsByCurrency: {} });

    await omsService.getDashboardSummary();

    expect(apiMock.get).toHaveBeenCalledWith('/oms/dashboard/summary', { params: undefined });
  });

  it('passes the optional dashboard window to the API', async () => {
    apiMock.get.mockResolvedValue({ totalsByCurrency: {} });

    await omsService.getDashboardSummary({ fromUtc: '2026-01-01', toUtc: '2026-02-01' });

    expect(apiMock.get).toHaveBeenCalledWith('/oms/dashboard/summary', {
      params: { fromUtc: '2026-01-01', toUtc: '2026-02-01' },
    });
  });

  it('lists orders from the paginated endpoint with the query parameters', async () => {
    apiMock.get.mockResolvedValue(envelope);

    const result = await omsService.getOrders({
      pageNumber: 2,
      pageSize: 10,
      status: 'Draft' as never,
      search: 'stationery',
    });

    expect(apiMock.get).toHaveBeenCalledWith('/oms/orders', {
      params: { pageNumber: 2, pageSize: 10, status: 'Draft', search: 'stationery' },
    });
    expect(result.totalCount).toBe(21);
    expect(result.pageNumber).toBe(2);
    expect(result.hasPreviousPage).toBe(true);
    expect(result.hasNextPage).toBe(true);
  });

  it('rejects a structurally invalid orders payload instead of coercing it', async () => {
    apiMock.get.mockResolvedValue({ unexpected: true });

    await expect(omsService.getOrders()).rejects.toThrow(
      'Unexpected response shape from the orders API.',
    );
  });

  it('creates an order without sending server-owned fields', async () => {
    apiMock.post.mockResolvedValue({ id: 'order-9', orderNumber: 'ORD-2026-000009' });

    await omsService.createOrder({
      title: 'Exam stationery',
      currency: 'KES',
      items: [{ description: 'Chalk', quantity: 2, unitPrice: 50 }],
    });

    expect(apiMock.post).toHaveBeenCalledTimes(1);
    const [url, body] = apiMock.post.mock.calls[0];
    expect(url).toBe('/oms/orders');
    expect(body).toEqual({
      title: 'Exam stationery',
      currency: 'KES',
      items: [{ description: 'Chalk', quantity: 2, unitPrice: 50 }],
    });
    // Order number, tenant, creator and total are server-owned and must never be sent.
    expect(body).not.toHaveProperty('orderNumber');
    expect(body).not.toHaveProperty('tenantId');
    expect(body).not.toHaveProperty('requestedByUserId');
    expect(body).not.toHaveProperty('totalAmount');
  });

  it('adds an item through the order items endpoint', async () => {
    apiMock.post.mockResolvedValue({ id: 'item-1' });

    await omsService.addOrderItem('order-1', {
      itemCode: 'ITM-001',
      description: 'Chalk boxes',
      quantity: 10,
      unitPrice: 25,
    });

    expect(apiMock.post).toHaveBeenCalledWith('/oms/orders/order-1/items', {
      itemCode: 'ITM-001',
      description: 'Chalk boxes',
      quantity: 10,
      unitPrice: 25,
    });
  });

  it('removes an item through the order items endpoint', async () => {
    apiMock.delete.mockResolvedValue({ id: 'item-1' });

    await omsService.removeOrderItem('order-1', 'item-1');

    expect(apiMock.delete).toHaveBeenCalledWith('/oms/orders/order-1/items/item-1');
  });

  it('submits an order through the submit endpoint', async () => {
    apiMock.post.mockResolvedValue({ id: 'order-1', status: 'Submitted' });

    await omsService.submitOrder('order-1');

    expect(apiMock.post).toHaveBeenCalledWith('/oms/orders/order-1/submit');
  });

  it('cancels the creator-owned order with the required reason', async () => {
    apiMock.post.mockResolvedValue({ id: 'order-1', status: 'Cancelled' });

    await omsService.cancelOwnOrder('order-1', 'Duplicate request');

    expect(apiMock.post).toHaveBeenCalledWith('/oms/orders/order-1/cancel', {
      reason: 'Duplicate request',
    });
  });

  it('uses the separate cancel-any endpoint for administrator cancellation', async () => {
    apiMock.post.mockResolvedValue({ id: 'order-1', status: 'Cancelled' });

    await omsService.cancelAnyOrder('order-1', 'Policy violation');

    expect(apiMock.post).toHaveBeenCalledWith('/oms/orders/order-1/cancel-any', {
      reason: 'Policy violation',
    });
  });
});

describe('normalizeOmsPagedResult', () => {
  it('passes through a well-formed PagedResult envelope', () => {
    const result = normalizeOmsPagedResult(envelope as never);

    expect(result).toEqual(envelope);
  });

  it('wraps a bare array defensively', () => {
    const result = normalizeOmsPagedResult([{ id: 'order-1' }] as never);

    expect(result.items).toHaveLength(1);
    expect(result.totalCount).toBe(1);
    expect(result.totalPages).toBe(1);
    expect(result.hasNextPage).toBe(false);
  });

  it('derives missing pagination fields instead of showing corrupt data', () => {
    const result = normalizeOmsPagedResult({
      items: [{ id: 'a' }, { id: 'b' }],
      totalCount: 5,
      pageSize: 2,
    } as never);

    expect(result.totalPages).toBe(3);
    expect(result.pageNumber).toBe(1);
    expect(result.hasNextPage).toBe(true);
    expect(result.hasPreviousPage).toBe(false);
  });

  it('throws for payloads that are neither an array nor an items envelope', () => {
    expect(() => normalizeOmsPagedResult({ nope: 1 } as never)).toThrow(
      'Unexpected response shape from the orders API.',
    );
    expect(() => normalizeOmsPagedResult(null as never)).toThrow(
      'Unexpected response shape from the orders API.',
    );
  });
});

describe('omsService — getOrder', () => {
  it('retrieves a single order by id', async () => {
    apiMock.get.mockResolvedValue({ id: 'order-1' });

    await omsService.getOrder('order-1');

    expect(apiMock.get).toHaveBeenCalledWith('/oms/orders/order-1');
  });
});
