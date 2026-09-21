import React, { useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Grid,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  Alert,
  Chip,
  Divider,
  Tooltip,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Add as AddIcon,
  Delete as DeleteIcon,
  Send as SendIcon,
  CancelOutlined as CancelIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { useSnackbar } from 'notistack';
import {
  omsService,
  OmsOrderStatus,
  OmsOrder,
  OmsOrderItem,
  AddOmsOrderItemRequest,
  isOmsOrderCancellable,
  isOmsOrderEditable,
  isOmsOrderSubmittable,
} from '../../services/oms.service';
import { useAuth } from '../../hooks/useAuth';
import { canManageOmsOrders, canCancelOwnOmsOrder, canCancelAnyOmsOrder, canViewOmsOrders } from '../../utils/roles';
import { normalizeError } from '../../utils/errors';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';
import { EmptyState } from '../../components/Common/EmptyState';
import { formatOmsMoney } from './OmsDashboard';
import { omsStatusColorMap } from './OmsOrders';

/** Server business errors (BUSINESS_RULE_VIOLATION etc.) carry the most
 *  specific text in serverMessage — prefer it for action feedback. */
export const getActionError = (error: unknown): string => {
  const normalized = normalizeError(error);
  return normalized.serverMessage || normalized.message;
};

type ItemFormState = {
  itemCode: string;
  description: string;
  quantity: string;
  unitPrice: string;
};

const EMPTY_ITEM_FORM: ItemFormState = { itemCode: '', description: '', quantity: '1', unitPrice: '0' };

/** Client-side mirror of AddOrderItemCommandValidator — the API stays
 *  authoritative; this only prevents avoidable round-trips. */
export const validateItemForm = (form: ItemFormState): string | null => {
  if (!form.description.trim()) return 'Item description is required.';
  if (form.description.trim().length > 500) return 'Item description must not exceed 500 characters.';
  if (form.itemCode.trim().length > 50) return 'Item code must not exceed 50 characters.';
  const quantity = Number(form.quantity);
  if (!Number.isInteger(quantity) || quantity <= 0) return 'Quantity must be an integer greater than zero.';
  const unitPrice = Number(form.unitPrice);
  if (Number.isNaN(unitPrice) || unitPrice < 0) return 'Unit price must not be negative.';
  return null;
};

export const OmsOrderDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();
  const canView = canViewOmsOrders(user?.roles);

  const [addItemOpen, setAddItemOpen] = useState(false);
  const [itemForm, setItemForm] = useState<ItemFormState>(EMPTY_ITEM_FORM);
  const [itemFormError, setItemFormError] = useState<string | null>(null);
  const [removeItemTarget, setRemoveItemTarget] = useState<OmsOrderItem | null>(null);
  const [submitOpen, setSubmitOpen] = useState(false);
  const [cancelMode, setCancelMode] = useState<'own' | 'any' | null>(null);
  const [cancelReason, setCancelReason] = useState('');
  const [cancelError, setCancelError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const { data: order, isLoading, isError, error, refetch, isRefetching } = useQuery({
    queryKey: ['oms-order', id],
    queryFn: () => omsService.getOrder(id!),
    // Only authorized users trigger the request; the API remains authoritative.
    enabled: !!id && canView,
  });

  const invalidateOrderData = () => {
    queryClient.invalidateQueries({ queryKey: ['oms-order', id] });
    queryClient.invalidateQueries({ queryKey: ['oms-orders'] });
    queryClient.invalidateQueries({ queryKey: ['oms-dashboard-summary'] });
  };

  // ── Mutations (the API remains authoritative for every state change) ──────
  const addItemMutation = useMutation({
    mutationFn: (request: AddOmsOrderItemRequest) => omsService.addOrderItem(id!, request),
    onSuccess: () => {
      invalidateOrderData();
      closeAddItemDialog();
      enqueueSnackbar('Item added to order.', { variant: 'success' });
    },
    onError: (err) => setItemFormError(getActionError(err)),
  });

  const removeItemMutation = useMutation({
    mutationFn: (itemId: string) => omsService.removeOrderItem(id!, itemId),
    onSuccess: () => {
      invalidateOrderData();
      setRemoveItemTarget(null);
      enqueueSnackbar('Item removed from order.', { variant: 'success' });
    },
    onError: (err) => {
      setRemoveItemTarget(null);
      setActionError(getActionError(err));
    },
  });

  const submitMutation = useMutation({
    mutationFn: () => omsService.submitOrder(id!),
    onSuccess: () => {
      invalidateOrderData();
      setSubmitOpen(false);
      enqueueSnackbar('Order submitted for approval.', { variant: 'success' });
    },
    onError: (err) => {
      setSubmitOpen(false);
      setActionError(getActionError(err));
    },
  });

  const cancelMutation = useMutation({
    mutationFn: (mode: 'own' | 'any') =>
      mode === 'own'
        ? omsService.cancelOwnOrder(id!, cancelReason.trim())
        : omsService.cancelAnyOrder(id!, cancelReason.trim()),
    onSuccess: (_data, mode) => {
      invalidateOrderData();
      closeCancelDialog();
      enqueueSnackbar(
        mode === 'any' ? 'Order cancelled (administrator action).' : 'Order cancelled.',
        { variant: 'success' },
      );
    },
    onError: (err) => {
      // Keep the dialog open so the server error (authorization or state
      // change) is visible next to the reason field.
      setCancelError(getActionError(err));
    },
  });

  const closeAddItemDialog = () => {
    setAddItemOpen(false);
    setItemForm(EMPTY_ITEM_FORM);
    setItemFormError(null);
  };

  const closeCancelDialog = () => {
    setCancelMode(null);
    setCancelReason('');
    setCancelError(null);
  };

  const handleAddItemSubmit = () => {
    const validationError = validateItemForm(itemForm);
    if (validationError) {
      setItemFormError(validationError);
      return;
    }
    setItemFormError(null);
    addItemMutation.mutate({
      itemCode: itemForm.itemCode.trim() || undefined,
      description: itemForm.description.trim(),
      quantity: Number(itemForm.quantity),
      unitPrice: Number(itemForm.unitPrice),
    });
  };

  const handleCancelSubmit = () => {
    if (!cancelMode) return;
    if (!cancelReason.trim()) {
      // Mirrors CancelOrderCommandValidator; the server enforces the same rule.
      setCancelError('A cancellation reason is required.');
      return;
    }
    setCancelError(null);
    cancelMutation.mutate(cancelMode);
  };

  // ── Permission-aware actions (UI affordances only; API is authoritative) ──
  const canManage = canManageOmsOrders(user?.roles);
  const isCreator =
    !!user?.id && !!order && order.requestedByUserId.toLowerCase() === user.id.toLowerCase();
  const canEditItems = canManage && isOmsOrderEditable(order?.status);
  const canSubmit = canManage && isOmsOrderSubmittable(order?.status);
  const canCancelOwn = canCancelOwnOmsOrder(user?.roles) && isCreator && isOmsOrderCancellable(order?.status);
  const canCancelAny = canCancelAnyOmsOrder(user?.roles) && !isCreator && isOmsOrderCancellable(order?.status);

  const normalizedError = isError ? normalizeError(error) : null;

  if (!canView) {
    return (
      <EmptyState
        title="Order unavailable"
        description="You do not have permission to view OMS orders. Please contact your administrator if you believe this is a mistake."
      />
    );
  }

  if (isLoading) {
    return <LoadingSpinner message="Loading order..." />;
  }

  if (isError || !order) {
    const notFound = normalizedError?.statusCode === 404;
    return (
      <Box>
        <Button startIcon={<ArrowBackIcon />} sx={{ mb: 2 }} onClick={() => navigate('/oms/orders')}>
          Back to Orders
        </Button>
        {notFound ? (
          <EmptyState
            title="Order not found"
            description="This order does not exist, may have been removed, or belongs to another school. The request was rejected by the server."
            actionText="Back to Orders"
            onAction={() => navigate('/oms/orders')}
          />
        ) : (
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={() => refetch()}>
                Retry
              </Button>
            }
          >
            {normalizedError?.message ?? 'Failed to load the order.'}
          </Alert>
        )}
      </Box>
    );
  }

  const statusColor = omsStatusColorMap[order.statusName] ?? 'default';

  return (
    <Box>
      <Button startIcon={<ArrowBackIcon />} sx={{ mb: 2 }} onClick={() => navigate('/oms/orders')}>
        Back to Orders
      </Button>

      <Paper sx={{ p: 2.5, mb: 2 }}>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'center', gap: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, minWidth: 0 }}>
            <Box>
              <Typography variant="h5" fontWeight={600} data-testid="order-number">
                {order.orderNumber}
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 0.5 }}>
                <Chip
                  label={order.statusName}
                  size="small"
                  color={statusColor}
                  sx={{ fontWeight: 500 }}
                  data-testid="order-status"
                />
                <Typography variant="body2" color="textSecondary" noWrap>
                  {order.title}
                </Typography>
              </Box>
            </Box>
          </Box>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            <Tooltip title="Refresh">
              <Button variant="outlined" startIcon={<RefreshIcon />} onClick={() => refetch()} disabled={isRefetching}>
                Refresh
              </Button>
            </Tooltip>
            {canEditItems && (
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={() => {
                  setItemForm(EMPTY_ITEM_FORM);
                  setItemFormError(null);
                  setAddItemOpen(true);
                }}
              >
                Add Item
              </Button>
            )}
            {canSubmit && (
              <Tooltip
                describeChild
                title={order.itemCount === 0 ? 'Add at least one item before submitting' : 'Submit this Draft order for approval'}
              >
                <Button
                  variant="contained"
                  color="primary"
                  startIcon={<SendIcon />}
                  onClick={() => setSubmitOpen(true)}
                  disabled={order.itemCount === 0 || submitMutation.isPending}
                >
                  Submit
                </Button>
              </Tooltip>
            )}
            {canCancelOwn && (
              <Tooltip describeChild title="Cancel this order (you created it)">
                <Button
                  variant="outlined"
                  color="error"
                  startIcon={<CancelIcon />}
                  onClick={() => {
                    setCancelReason('');
                    setCancelError(null);
                    setCancelMode('own');
                  }}
                >
                  Cancel Order
                </Button>
              </Tooltip>
            )}
            {canCancelAny && (
              <Tooltip describeChild title="Administrator cancellation of any non-final order">
                <Button
                  variant="outlined"
                  color="error"
                  startIcon={<CancelIcon />}
                  onClick={() => {
                    setCancelReason('');
                    setCancelError(null);
                    setCancelMode('any');
                  }}
                >
                  Cancel (Administrator)
                </Button>
              </Tooltip>
            )}
          </Box>
        </Box>
      </Paper>

      {actionError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setActionError(null)}>
          {actionError}
        </Alert>
      )}

      <Grid container spacing={2}>
        <Grid item xs={12} md={5}>
          <Paper sx={{ p: 2.5, height: '100%' }}>
            <Typography variant="h6" fontWeight={600} gutterBottom>
              Order Information
            </Typography>
            <Divider sx={{ mb: 2 }} />
            <Box sx={{ display: 'grid', gap: 1.5 }}>
              <DetailRow label="Order Number" value={order.orderNumber} />
              <DetailRow label="Title" value={order.title} />
              <DetailRow label="Description" value={order.description || '—'} />
              <DetailRow
                label="Total"
                value={`${formatOmsMoney(order.totalAmount, order.currency)} (${order.currency})`}
              />
              <DetailRow
                label="Required By"
                value={order.requiredByDate ? new Date(order.requiredByDate).toLocaleDateString() : '—'}
              />
              <DetailRow label="Items" value={String(order.itemCount)} />
              <DetailRow label="Created" value={new Date(order.createdAt).toLocaleString()} />
              <DetailRow label="Last Updated" value={new Date(order.updatedAt).toLocaleString()} />
            </Box>
          </Paper>
        </Grid>

        <Grid item xs={12} md={7}>
          <Paper sx={{ p: 2.5, height: '100%' }}>
            <Typography variant="h6" fontWeight={600} gutterBottom>
              Status Information
            </Typography>
            <Divider sx={{ mb: 2 }} />
            <Box sx={{ display: 'grid', gap: 1.5 }}>
              <DetailRow label="Requested By" value={order.requestedByUserId} />
              {order.submittedAtUtc && (
                <DetailRow
                  label="Submitted"
                  value={`${new Date(order.submittedAtUtc).toLocaleString()} by ${order.requestedByUserId}`}
                />
              )}
              {order.approvedAtUtc && (
                <DetailRow
                  label="Approved"
                  value={`${new Date(order.approvedAtUtc).toLocaleString()} by ${order.approvedByUserId ?? '—'}`}
                />
              )}
              {order.approvalRemarks && <DetailRow label="Approval Remarks" value={order.approvalRemarks} />}
              {order.rejectedAtUtc && (
                <DetailRow
                  label="Rejected"
                  value={`${new Date(order.rejectedAtUtc).toLocaleString()} by ${order.rejectedByUserId ?? '—'}`}
                />
              )}
              {order.rejectionRemarks && <DetailRow label="Rejection Remarks" value={order.rejectionRemarks} />}
              {order.cancelledAtUtc && (
                <DetailRow
                  label="Cancelled"
                  value={`${new Date(order.cancelledAtUtc).toLocaleString()} by ${order.cancelledByUserId ?? '—'}`}
                />
              )}
              {order.cancellationReason && <DetailRow label="Cancellation Reason" value={order.cancellationReason} />}
              {!order.submittedAtUtc && !order.cancelledAtUtc && order.status === OmsOrderStatus.Draft && (
                <Typography variant="body2" color="textSecondary">
                  This order is still a Draft — edit its items, then submit it for approval.
                </Typography>
              )}
            </Box>
          </Paper>
        </Grid>

        <Grid item xs={12}>
          <Paper sx={{ p: 2.5 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="h6" fontWeight={600}>
                Items ({order.itemCount})
              </Typography>
              {canEditItems && order.itemCount > 0 && (
                <Typography variant="caption" color="textSecondary">
                  Items can be edited only while the order is in Draft.
                </Typography>
              )}
            </Box>
            <Divider sx={{ mb: 2 }} />
            {order.items.length === 0 ? (
              <Typography variant="body2" color="textSecondary" sx={{ py: 2, textAlign: 'center' }}>
                This order has no items yet.{' '}
                {canEditItems ? 'Use “Add Item” to add the first one.' : 'Items appear here once added.'}
              </Typography>
            ) : (
              <TableContainer>
                <Table size="small" aria-label="order items table">
                  <TableHead>
                    <TableRow>
                      <TableCell>#</TableCell>
                      <TableCell>Item Code</TableCell>
                      <TableCell>Description</TableCell>
                      <TableCell align="right">Quantity</TableCell>
                      <TableCell align="right">Unit Price</TableCell>
                      <TableCell align="right">Line Total</TableCell>
                      {canEditItems && <TableCell align="right">Actions</TableCell>}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {order.items.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell>{item.rowNumber}</TableCell>
                        <TableCell>{item.itemCode || '—'}</TableCell>
                        <TableCell>{item.description}</TableCell>
                        <TableCell align="right">{item.quantity}</TableCell>
                        <TableCell align="right">{formatOmsMoney(item.unitPrice, order.currency)}</TableCell>
                        <TableCell align="right">{formatOmsMoney(item.lineTotal, order.currency)}</TableCell>
                        {canEditItems && (
                          <TableCell align="right">
                            <Tooltip title="Remove item">
                              <IconButton
                                size="small"
                                color="error"
                                aria-label={`Remove item ${item.description}`}
                                onClick={() => setRemoveItemTarget(item)}
                              >
                                <DeleteIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          </TableCell>
                        )}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
            <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 2 }}>
              <Typography variant="subtitle1" fontWeight={600}>
                Total ({order.currency}): {formatOmsMoney(order.totalAmount, order.currency)}
              </Typography>
            </Box>
            <Typography variant="caption" color="textSecondary" sx={{ display: 'block', textAlign: 'right' }}>
              Totals are maintained by the server.
            </Typography>
          </Paper>
        </Grid>

      </Grid>

      {/* ── Add Item dialog (Draft only; server remains authoritative) ── */}
      <Dialog open={addItemOpen} onClose={closeAddItemDialog} maxWidth="sm" fullWidth>
        <DialogTitle>Add Item</DialogTitle>
        <DialogContent>
          {itemFormError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {itemFormError}
            </Alert>
          )}
          <Box sx={{ display: 'grid', gap: 2, pt: 0.5 }}>
            <TextField
              label="Item Code (optional)"
              size="small"
              value={itemForm.itemCode}
              onChange={(e) => setItemForm({ ...itemForm, itemCode: e.target.value })}
              inputProps={{ maxLength: 50 }}
            />
            <TextField
              label="Description"
              size="small"
              required
              value={itemForm.description}
              onChange={(e) => setItemForm({ ...itemForm, description: e.target.value })}
              inputProps={{ maxLength: 500 }}
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Quantity"
                size="small"
                type="number"
                required
                value={itemForm.quantity}
                onChange={(e) => setItemForm({ ...itemForm, quantity: e.target.value })}
                inputProps={{ min: 1, step: 1 }}
                sx={{ width: 140 }}
              />
              <TextField
                label="Unit Price"
                size="small"
                type="number"
                required
                value={itemForm.unitPrice}
                onChange={(e) => setItemForm({ ...itemForm, unitPrice: e.target.value })}
                inputProps={{ min: 0, step: 0.01 }}
                sx={{ width: 180 }}
              />
            </Box>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeAddItemDialog}>Cancel</Button>
          <Button onClick={handleAddItemSubmit} variant="contained" disabled={addItemMutation.isPending}>
            {addItemMutation.isPending ? 'Adding...' : 'Add Item'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Remove Item confirmation ── */}
      <Dialog open={!!removeItemTarget} onClose={() => setRemoveItemTarget(null)}>
        <DialogTitle>Remove Item</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Remove “{removeItemTarget?.description}” from this order? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRemoveItemTarget(null)}>Keep Item</Button>
          <Button
            color="error"
            variant="contained"
            disabled={removeItemMutation.isPending}
            onClick={() => removeItemTarget && removeItemMutation.mutate(removeItemTarget.id)}
          >
            {removeItemMutation.isPending ? 'Removing...' : 'Remove'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Submit confirmation (Draft only) ── */}
      <Dialog open={submitOpen} onClose={() => setSubmitOpen(false)}>
        <DialogTitle>Submit Order for Approval</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Submit order <strong>{order?.orderNumber}</strong> with {order?.itemCount} item(s) for approval?
            Submitted orders can no longer be edited.
          </DialogContentText>
          {order && order.itemCount === 0 && (
            <Alert severity="warning" sx={{ mt: 2 }}>
              This order has no items. The server will reject the submission until at least one item is added.
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSubmitOpen(false)}>Not Now</Button>
          <Button
            variant="contained"
            startIcon={<SendIcon />}
            disabled={submitMutation.isPending || (order?.itemCount ?? 0) === 0}
            onClick={() => submitMutation.mutate()}
          >
            {submitMutation.isPending ? 'Submitting...' : 'Submit Order'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Cancellation (creator “cancel” or administrator “cancel-any”) ── */}
      <Dialog open={!!cancelMode} onClose={closeCancelDialog} maxWidth="sm" fullWidth>
        <DialogTitle>
          {cancelMode === 'any' ? 'Cancel Order (Administrator)' : 'Cancel Order'}
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            {cancelMode === 'any'
              ? 'You are cancelling an order created by another user. This action is recorded in the audit trail.'
              : 'Cancelling an order is permanent and requires a reason.'}
          </DialogContentText>
          {cancelError && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {cancelError}
            </Alert>
          )}
          <TextField
            label="Cancellation reason"
            required
            multiline
            minRows={3}
            fullWidth
            margin="normal"
            value={cancelReason}
            onChange={(e) => setCancelReason(e.target.value)}
            inputProps={{ maxLength: 1000 }}
            error={!!cancelError && !cancelReason.trim()}
            helperText={`${cancelReason.trim().length}/1000 characters`}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={closeCancelDialog}>Keep Order</Button>
          <Button
            color="error"
            variant="contained"
            disabled={cancelMutation.isPending || !cancelReason.trim()}
            onClick={handleCancelSubmit}
          >
            {cancelMutation.isPending ? 'Cancelling...' : 'Cancel Order'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

/** Simple label/value row used by the info cards. */
const DetailRow: React.FC<{ label: string; value: string }> = ({ label, value }) => (
  <Box sx={{ display: 'flex', gap: 2, alignItems: 'baseline' }}>
    <Typography variant="body2" color="textSecondary" sx={{ minWidth: 140, flexShrink: 0 }}>
      {label}
    </Typography>
    <Typography variant="body2" sx={{ wordBreak: 'break-word', whiteSpace: 'pre-wrap' }} title={value}>
      {value}
    </Typography>
  </Box>
);

