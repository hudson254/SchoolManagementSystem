// ────────────────────────────────────────────────────────────────────────────
// OMS (Order Management System) API client — Phase 2D.
//
// Mirrors the v1 OMS API contracts implemented in Phase 2C:
//   src/SMS.API/Controllers/v1/Oms/OmsOrdersController.cs
//   src/SMS.API/Controllers/v1/Oms/OmsDashboardController.cs
//   src/SMS.Application/Features/OMS/Dtos/OrderDtos.cs / OmsDtos.cs
//
// The backend is authoritative for order numbers, tenant ids, creator ids,
// totals and currency rules. This client never computes or overrides any of
// them — it only relays server values for display.
// ────────────────────────────────────────────────────────────────────────────
import { api } from './api';

/**
 * Mirrors SMS.Domain.Enums.OrderStatus.
 *
 * Modeled as a const object + string-literal union (rather than a TS enum)
 * because the API serializes the value as a plain string: this keeps the
 * declared type identical to the wire format, so responses and fixtures can
 * be typed without casts. `OmsOrderStatus.Draft` still works as a value.
 */
export const OmsOrderStatus = {
  Draft: 'Draft',
  Submitted: 'Submitted',
  PendingApproval: 'PendingApproval',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
} as const;

export type OmsOrderStatus = (typeof OmsOrderStatus)[keyof typeof OmsOrderStatus];

/** Mirrors SMS.Application.Features.OMS.Dtos.OrderItemDto. */
export type OmsOrderItem = {
  id: string;
  orderId: string;
  rowNumber: number;
  itemCode?: string | null;
  description: string;
  quantity: number;
  unitPrice: number;
  /** Server-computed (Quantity × UnitPrice, half-even rounding). Display only. */
  lineTotal: number;
};

/** Mirrors SMS.Application.Features.OMS.Dtos.OrderDto. */
export type OmsOrder = {
  id: string;
  orderNumber: string;
  title: string;
  description?: string | null;
  status: OmsOrderStatus;
  statusName: string;
  requestedByUserId: string;
  approvedByUserId?: string | null;
  approvedAtUtc?: string | null;
  approvalRemarks?: string | null;
  rejectedByUserId?: string | null;
  rejectedAtUtc?: string | null;
  rejectionRemarks?: string | null;
  cancelledByUserId?: string | null;
  cancelledAtUtc?: string | null;
  cancellationReason?: string | null;
  submittedAtUtc?: string | null;
  requiredByDate?: string | null;
  roadAccountId?: string | null;
  /** Server-maintained total. Display only — never recomputed in the browser. */
  totalAmount: number;
  currency: string;
  createdAt: string;
  updatedAt: string;
  itemCount: number;
  items: OmsOrderItem[];
};

/** Mirrors SMS.Application.Features.OMS.Dtos.OmsDashboardSummaryDto. */
export type OmsDashboardSummary = {
  draftCount: number;
  submittedCount: number;
  pendingApprovalCount: number;
  approvedCount: number;
  rejectedCount: number;
  cancelledCount: number;
  totalOrders: number;
  /** Server-grouped order totals by currency (ISO-4217 code → amount). */
  totalsByCurrency: Record<string, number>;
};

/**
 * Mirrors SMS.Application.Common.PagedResult<T> (camelCase). Consumed only
 * through `normalizeOmsPagedResult`, which keeps malformed envelopes from
 * silently corrupting the list UI.
 */
export type OmsPagedResult<T> = {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export type GetOmsOrdersParams = {
  status?: OmsOrderStatus;
  requestedByUserId?: string;
  roadAccountId?: string;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
};

export type CreateOmsOrderItemRequest = {
  itemCode?: string;
  description: string;
  quantity: number;
  unitPrice: number;
};

/** Mirrors CreateOrderCommand (server generates number/tenant/creator/total). */
export type CreateOmsOrderRequest = {
  title: string;
  description?: string;
  /** 3-letter ISO-4217 code; server default is KES. */
  currency?: string;
  /** ISO date string (yyyy-MM-dd) or omitted. */
  requiredByDate?: string;
  /** Optional initial line items; more can be added while in Draft. */
  items: CreateOmsOrderItemRequest[];
};

/** Mirrors AddOrderItemRequest. */
export type AddOmsOrderItemRequest = {
  itemCode?: string;
  description: string;
  quantity: number;
  unitPrice: number;
};

// ────────────────────────────────────────────────────────────────────────────
// Workflow helpers (display only — the API remains the security boundary;
// these only decide which affordances are worth showing).
// ────────────────────────────────────────────────────────────────────────────

/** Statuses from which the API still accepts cancellation. */
export const OMS_CANCELLABLE_STATUSES: readonly OmsOrderStatus[] = [
  OmsOrderStatus.Draft,
  OmsOrderStatus.Submitted,
  OmsOrderStatus.PendingApproval,
];

/** Draft is the only status that accepts item edits. */
export const isOmsOrderEditable = (status: OmsOrderStatus | undefined): boolean =>
  status === OmsOrderStatus.Draft;

/** Only Draft orders can be submitted. */
export const isOmsOrderSubmittable = (status: OmsOrderStatus | undefined): boolean =>
  status === OmsOrderStatus.Draft;

/** Approved / Rejected / Cancelled are terminal; no further actions. */
export const isOmsOrderCancellable = (status: OmsOrderStatus | undefined): boolean =>
  status !== undefined && OMS_CANCELLABLE_STATUSES.includes(status);

/**
 * Normalizes the orders-list response into a strict OmsPagedResult.
 *
 * Handles:
 *  - the real envelope (PagedResult<OrderDto>, camelCase),
 *  - a bare array defensively (wrapped as a single page),
 *  - structurally invalid payloads (rejected with a clear error so the page
 *    renders its error state instead of invisible data corruption).
 */
export function normalizeOmsPagedResult(
  data: OmsPagedResult<OmsOrder> | OmsOrder[] | unknown,
): OmsPagedResult<OmsOrder> {
  if (Array.isArray(data)) {
    return {
      items: data,
      totalCount: data.length,
      pageNumber: 1,
      pageSize: data.length || 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };
  }

  if (
    data !== null &&
    typeof data === 'object' &&
    Array.isArray((data as { items?: unknown }).items)
  ) {
    const envelope = data as Partial<OmsPagedResult<OmsOrder>>;
    const totalCount =
      typeof envelope.totalCount === 'number' ? envelope.totalCount : envelope.items!.length;
    const pageNumber = typeof envelope.pageNumber === 'number' ? envelope.pageNumber : 1;
    const pageSize =
      typeof envelope.pageSize === 'number' ? envelope.pageSize : envelope.items!.length || 1;
    const totalPages =
      typeof envelope.totalPages === 'number'
        ? envelope.totalPages
        : pageSize > 0
          ? Math.ceil(totalCount / pageSize)
          : 0;
    return {
      items: envelope.items!,
      totalCount,
      pageNumber,
      pageSize,
      totalPages,
      hasPreviousPage: envelope.hasPreviousPage ?? pageNumber > 1,
      hasNextPage: envelope.hasNextPage ?? pageNumber < totalPages,
    };
  }

  throw new Error('Unexpected response shape from the orders API.');
}

// ────────────────────────────────────────────────────────────────────────────
// Service
// ────────────────────────────────────────────────────────────────────────────

export const omsService = {
  /** GET /oms/dashboard/summary — OmsDashboardSummaryQuery via OmsDashboardController. */
  getDashboardSummary: (params?: { fromUtc?: string; toUtc?: string }) =>
    api.get<OmsDashboardSummary>('/oms/dashboard/summary', { params }),

  /** GET /oms/orders — paginated, filterable list (PagedResult<OrderDto>). */
  getOrders: (params: GetOmsOrdersParams = {}) =>
    api.get<OmsPagedResult<OmsOrder>>('/oms/orders', { params }).then(normalizeOmsPagedResult),

  /** GET /oms/orders/{id} — single order with line items. */
  getOrder: (orderId: string) => api.get<OmsOrder>(`/oms/orders/${orderId}`),

  /** POST /oms/orders — creates a Draft; returns the server-created order. */
  createOrder: (data: CreateOmsOrderRequest) => api.post<OmsOrder>('/oms/orders', data),

  /** POST /oms/orders/{id}/items — Draft-only; returns the created item. */
  addOrderItem: (orderId: string, data: AddOmsOrderItemRequest) =>
    api.post<OmsOrderItem>(`/oms/orders/${orderId}/items`, data),

  /** DELETE /oms/orders/{id}/items/{itemId} — Draft-only; returns the removed item. */
  removeOrderItem: (orderId: string, itemId: string) =>
    api.delete<OmsOrderItem>(`/oms/orders/${orderId}/items/${itemId}`),

  /** POST /oms/orders/{id}/submit — Draft-only; requires at least one item. */
  submitOrder: (orderId: string) => api.post<OmsOrder>(`/oms/orders/${orderId}/submit`),

  /** POST /oms/orders/{id}/cancel — creator-only cancellation (reason required). */
  cancelOwnOrder: (orderId: string, reason: string) =>
    api.post<OmsOrder>(`/oms/orders/${orderId}/cancel`, { reason }),

  /** POST /oms/orders/{id}/cancel-any — Administrator cancellation (reason required). */
  cancelAnyOrder: (orderId: string, reason: string) =>
    api.post<OmsOrder>(`/oms/orders/${orderId}/cancel-any`, { reason }),
};


