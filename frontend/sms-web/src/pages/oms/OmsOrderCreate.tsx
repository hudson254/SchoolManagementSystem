import React, { useState } from 'react';
import {
  Box,
  Button,
  Paper,
  TextField,
  Typography,
  Alert,
  IconButton,
  Tooltip,
  Divider,
  Grid,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Add as AddIcon,
  Delete as DeleteIcon,
  Save as SaveIcon,
} from '@mui/icons-material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useSnackbar } from 'notistack';
import { omsService, CreateOmsOrderItemRequest } from '../../services/oms.service';
import { useAuth } from '../../hooks/useAuth';
import { canManageOmsOrders } from '../../utils/roles';
import { normalizeError, getFieldErrors } from '../../utils/errors';
import { EmptyState } from '../../components/Common/EmptyState';

type ItemRow = {
  itemCode: string;
  description: string;
  quantity: string;
  unitPrice: string;
};

const EMPTY_ITEM_ROW: ItemRow = { itemCode: '', description: '', quantity: '1', unitPrice: '0' };

/** Field-level validation mirroring CreateOrderCommandValidator. The server
 *  stays authoritative — this only prevents avoidable round-trips. */
export const validateCreateOrderForm = (
  title: string,
  currency: string,
  items: ItemRow[],
): Record<string, string> => {
  const errors: Record<string, string> = {};
  if (!title.trim()) errors.title = 'Order title is required.';
  else if (title.trim().length > 200) errors.title = 'Order title must not exceed 200 characters.';
  if (!currency.trim()) errors.currency = 'Currency is required.';
  else if (currency.trim().length !== 3) errors.currency = 'Currency must be a 3-letter ISO-4217 code.';
  items.forEach((item, index) => {
    if (!item.description.trim()) errors[`items.${index}.description`] = 'Item description is required.';
    else if (item.description.trim().length > 500)
      errors[`items.${index}.description`] = 'Item description must not exceed 500 characters.';
    if (item.itemCode.trim().length > 50)
      errors[`items.${index}.itemCode`] = 'Item code must not exceed 50 characters.';
    const quantity = Number(item.quantity);
    if (!Number.isInteger(quantity) || quantity <= 0)
      errors[`items.${index}.quantity`] = 'Item quantity must be greater than zero.';
    const unitPrice = Number(item.unitPrice);
    if (Number.isNaN(unitPrice) || unitPrice < 0)
      errors[`items.${index}.unitPrice`] = 'Item unit price must not be negative.';
  });
  return errors;
};

export const OmsOrderCreate: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [currency, setCurrency] = useState('KES');
  const [requiredByDate, setRequiredByDate] = useState('');
  const [items, setItems] = useState<ItemRow[]>([]);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: (request: {
      title: string;
      description?: string;
      currency: string;
      requiredByDate?: string;
      items: CreateOmsOrderItemRequest[];
    }) => omsService.createOrder(request),
    onSuccess: (order) => {
      // The server generated the order number, id, tenant, creator and totals.
      queryClient.invalidateQueries({ queryKey: ['oms-orders'] });
      queryClient.invalidateQueries({ queryKey: ['oms-dashboard-summary'] });
      enqueueSnackbar(`Order ${order.orderNumber} created.`, { variant: 'success' });
      navigate(`/oms/orders/${order.id}`, { replace: true });
    },
    onError: (err) => {
      const normalized = normalizeError(err);
      setServerError(normalized.serverMessage || normalized.message);
      const fieldErrors = getFieldErrors(err);
      if (fieldErrors && Object.keys(fieldErrors).length > 0) {
        const mapped: Record<string, string> = {};
        Object.entries(fieldErrors).forEach(([key, messages]) => {
          // Map known top-level keys onto form fields; anything else is shown
          // in the summary alert.
          if (/title/i.test(key)) mapped.title = messages.join(' ');
          else if (/currency/i.test(key)) mapped.currency = messages.join(' ');
          else mapped[`server.${key}`] = messages.join(' ');
        });
        setFormErrors((prev) => ({ ...prev, ...mapped }));
      }
    },
  });

  const handleAddItemRow = () => setItems((prev) => [...prev, { ...EMPTY_ITEM_ROW }]);

  const handleRemoveItemRow = (index: number) =>
    setItems((prev) => prev.filter((_, i) => i !== index));

  const handleUpdateItemRow = (index: number, patch: Partial<ItemRow>) =>
    setItems((prev) => prev.map((row, i) => (i === index ? { ...row, ...patch } : row)));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setServerError(null);
    const validationErrors = validateCreateOrderForm(title, currency, items);
    if (Object.keys(validationErrors).length > 0) {
      setFormErrors(validationErrors);
      return;
    }
    setFormErrors({});
    createMutation.mutate({
      title: title.trim(),
      description: description.trim() || undefined,
      currency: currency.trim().toUpperCase(),
      requiredByDate: requiredByDate || undefined,
      // The server generates the order number, tenant, creator and totals.
      items: items.map((item) => ({
        itemCode: item.itemCode.trim() || undefined,
        description: item.description.trim(),
        quantity: Number(item.quantity),
        unitPrice: Number(item.unitPrice),
      })),
    });
  };

  if (!canManageOmsOrders(user?.roles)) {
    return (
      <EmptyState
        title="Order creation unavailable"
        description="You do not have permission to create OMS orders. Please contact your administrator if you believe this is a mistake."
      />
    );
  }

  return (
    <Box>
      <Button startIcon={<ArrowBackIcon />} sx={{ mb: 2 }} onClick={() => navigate('/oms/orders')}>
        Back to Orders
      </Button>

      <Paper sx={{ p: 3, maxWidth: 900, mx: 'auto' }} component="form" onSubmit={handleSubmit} noValidate>
        <Typography variant="h5" fontWeight={600}>
          New Order
        </Typography>
        <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
          Creates a Draft order. The order number is generated by the server; items can be added
          and edited while the order remains in Draft.
        </Typography>

        {serverError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setServerError(null)}>
            {serverError}
          </Alert>
        )}

        <Grid container spacing={2}>
          <Grid item xs={12}>
            <TextField
              label="Order Title"
              required
              fullWidth
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              error={!!formErrors.title}
              helperText={formErrors.title || 'A short summary of what is being requested (max 200 characters).'}
              inputProps={{ maxLength: 200 }}
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              label="Description"
              fullWidth
              multiline
              minRows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              error={!!formErrors.description}
              helperText={formErrors.description || 'Optional details for the approver (max 2000 characters).'}
              inputProps={{ maxLength: 2000 }}
            />
          </Grid>
          <Grid item xs={6} sm={4}>
            <TextField
              label="Currency"
              required
              fullWidth
              value={currency}
              onChange={(e) => setCurrency(e.target.value.toUpperCase())}
              error={!!formErrors.currency}
              helperText={formErrors.currency || 'ISO-4217 code — server default is KES.'}
              inputProps={{ maxLength: 3 }}
            />
          </Grid>
          <Grid item xs={6} sm={4}>
            <TextField
              label="Required By"
              type="date"
              fullWidth
              value={requiredByDate}
              onChange={(e) => setRequiredByDate(e.target.value)}
              InputLabelProps={{ shrink: true }}
              helperText="Optional target date."
            />
          </Grid>
        </Grid>

        <Divider sx={{ my: 3 }} />

        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
          <Typography variant="h6" fontWeight={600}>
            Initial Items (optional)
          </Typography>
          <Button size="small" startIcon={<AddIcon />} onClick={handleAddItemRow}>
            Add Item Row
          </Button>
        </Box>
        <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
          Items can also be added after creation while the order is in Draft.
        </Typography>

        {items.map((item, index) => (
          <Paper
            key={index}
            variant="outlined"
            role="group"
            aria-label={`Item ${index + 1}`}
            sx={{ p: 2, mb: 2 }}
          >
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="subtitle2">Item {index + 1}</Typography>
              <Tooltip title="Remove this row">
                <IconButton
                  size="small"
                  color="error"
                  aria-label={`Remove item row ${index + 1}`}
                  onClick={() => handleRemoveItemRow(index)}
                >
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            </Box>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={3}>
                <TextField
                  label="Item Code"
                  size="small"
                  fullWidth
                  value={item.itemCode}
                  onChange={(e) => handleUpdateItemRow(index, { itemCode: e.target.value })}
                  error={!!formErrors[`items.${index}.itemCode`]}
                  helperText={formErrors[`items.${index}.itemCode`]}
                  inputProps={{ maxLength: 50 }}
                />
              </Grid>
              <Grid item xs={12} sm={5}>
                <TextField
                  label="Description"
                  size="small"
                  required
                  fullWidth
                  value={item.description}
                  onChange={(e) => handleUpdateItemRow(index, { description: e.target.value })}
                  error={!!formErrors[`items.${index}.description`]}
                  helperText={formErrors[`items.${index}.description`]}
                  inputProps={{ maxLength: 500 }}
                />
              </Grid>
              <Grid item xs={6} sm={2}>
                <TextField
                  label="Quantity"
                  size="small"
                  type="number"
                  fullWidth
                  value={item.quantity}
                  onChange={(e) => handleUpdateItemRow(index, { quantity: e.target.value })}
                  error={!!formErrors[`items.${index}.quantity`]}
                  helperText={formErrors[`items.${index}.quantity`]}
                  inputProps={{ min: 1, step: 1 }}
                />
              </Grid>
              <Grid item xs={6} sm={2}>
                <TextField
                  label="Unit Price"
                  size="small"
                  type="number"
                  fullWidth
                  value={item.unitPrice}
                  onChange={(e) => handleUpdateItemRow(index, { unitPrice: e.target.value })}
                  error={!!formErrors[`items.${index}.unitPrice`]}
                  helperText={formErrors[`items.${index}.unitPrice`]}
                  inputProps={{ min: 0, step: 0.01 }}
                />
              </Grid>
            </Grid>
          </Paper>
        ))}

        {Object.keys(formErrors)
          .filter((key) => key.startsWith('server.'))
          .map((key) => (
            <Alert key={key} severity="error" sx={{ mb: 1 }}>
              {formErrors[key]}
            </Alert>
          ))}

        <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1, mt: 2 }}>
          <Button onClick={() => navigate('/oms/orders')} disabled={createMutation.isPending}>
            Cancel
          </Button>
          <Button
            type="submit"
            variant="contained"
            startIcon={<SaveIcon />}
            disabled={createMutation.isPending}
          >
            {createMutation.isPending ? 'Creating...' : 'Create Order'}
          </Button>
        </Box>

      </Paper>
    </Box>
  );

};
