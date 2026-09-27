import React, { useCallback, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Divider,
  Grid,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
// Named barrel import — see the note in RequestsDashboard.tsx for why deep
// default imports of MUI icons break the production bundle.
import {
  ArrowBack as ArrowBackIcon,
  Link as LinkIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import {
  omsRequestsService,
  OmsRequestAttachment,
  OmsRequestDetail,
} from '../../services/requests.service';
import { useAuth } from '../../hooks/useAuth';
import { normalizeError } from '../../utils/errors';
import {
  canAttachToOmsRequest,
  canCommentOnOmsRequest,
  canDeleteOmsRequestAttachment,
  getAvailableOmsRequestActions,
  isTerminalOmsRequestStatus,
  omsRequestStatusLabel,
  type OmsRequestActionDescriptor,
} from '../../utils/omsRequestLifecycle';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';
import { OmsRequestStatusChip, OmsRequestPriorityChip } from '../../components/omsRequests/RequestChips';
import { RequestStatusHistoryPanel } from '../../components/omsRequests/RequestStatusHistoryPanel';
import { RequestCommentsPanel } from '../../components/omsRequests/RequestCommentsPanel';
import { RequestAttachmentsPanel } from '../../components/omsRequests/RequestAttachmentsPanel';
import { RequestActionsBar } from '../../components/omsRequests/RequestActionsBar';
import type { RequestActionSubmitPayload } from '../../components/omsRequests/RequestActionsBar';

const formatDateTime = (value?: string | null): string => {
  if (!value) return 'â€”';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'â€”';
  return date.toLocaleString();
};

/**
 * Where a related SMS entity lives in this application. These are navigation
 * targets only: the backend remains the authority on whether the current user
 * may actually open the target, and the request workspace never mutates the
 * entity from here.
 */
const RELATED_ENTITY_ROUTES: Record<string, { label: string; path: (id: string) => string }> = {
  enrollment: { label: 'View Enrollment', path: (id) => `/students?enrollmentId=${id}` },
  accommodation: { label: 'View Accommodation', path: () => '/accommodation' },
  assignment: { label: 'View Assignment', path: (id) => `/assignments/${id}` },
};

const DetailRow: React.FC<{ label: string; children: React.ReactNode }> = ({ label, children }) => (
  <Grid item xs={12} sm={6} md={4}>
    <Typography variant="caption" color="textSecondary" display="block">
      {label}
    </Typography>
    <Typography variant="body2">{children}</Typography>
  </Grid>
);

/**
 * Full Request Detail.
 *
 * Concurrency policy: every lifecycle action mutates the request, so a 409 means
 * someone else changed it first. The workspace never overwrites newer server
 * state â€” it reports the conflict and refetches, letting the server's current
 * state replace the stale copy on screen.
 */
export const RequestDetailPage: React.FC = () => {
  const { requestId } = useParams<{ requestId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuth();

  const [actionError, setActionError] = useState<string | null>(null);
  const [conflictNotice, setConflictNotice] = useState<string | null>(null);

  const requestQuery = useQuery({
    queryKey: ['oms-request', requestId],
    queryFn: () => omsRequestsService.getRequest(requestId!),
    enabled: !!requestId,
    retry: false,
  });

  const attachmentsQuery = useQuery({
    queryKey: ['oms-request-attachments', requestId],
    queryFn: () => omsRequestsService.getAttachments(requestId!),
    enabled: !!requestId,
    retry: false,
  });

  const refreshRequest = useCallback(async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['oms-request', requestId] }),
      queryClient.invalidateQueries({ queryKey: ['oms-request-attachments', requestId] }),
      // The list and dashboard counts are now stale too.
      queryClient.invalidateQueries({ queryKey: ['oms-requests'] }),
      queryClient.invalidateQueries({ queryKey: ['oms-request-dashboard'] }),
    ]);
  }, [queryClient, requestId]);

  /**
   * Runs a lifecycle action. Error handling is deliberately explicit per status
   * code rather than collapsed into one generic message.
   */
  const runLifecycleAction = useCallback(
    async (descriptor: OmsRequestActionDescriptor, payload: RequestActionSubmitPayload) => {
      setActionError(null);
      setConflictNotice(null);
      const id = requestId!;

      try {
        switch (descriptor.key) {
          case 'submit':
            await omsRequestsService.submitRequest(id);
            break;
          case 'startReview':
            await omsRequestsService.startReview(id);
            break;
          case 'assign':
            await omsRequestsService.assignRequest(id, {
              assignedUserId: payload.assigneeId!,
              assignmentReason: payload.notes,
            });
            break;
          case 'reassign':
            await omsRequestsService.reassignRequest(id, {
              newAssignedUserId: payload.assigneeId!,
              reassignmentReason: payload.notes,
            });
            break;
          case 'approve':
            await omsRequestsService.approveRequest(id, payload.notes);
            break;
          case 'reject':
            await omsRequestsService.rejectRequest(id, payload.reason!);
            break;
          case 'returnForCorrection':
            await omsRequestsService.returnForCorrection(id, payload.reason!);
            break;
          case 'cancel':
            await omsRequestsService.cancelRequest(id, payload.notes);
            break;
          case 'complete':
            await omsRequestsService.completeRequest(id, payload.notes);
            break;
          case 'escalate':
            await omsRequestsService.escalateRequest(id, {
              escalationReason: payload.reason!,
            });
            break;
          default:
            return;
        }
        // Always re-read: the response DTO is the summary, not the full detail.
        await refreshRequest();
      } catch (error) {
        const normalized = normalizeError(error);
        const serverMessage = normalized.serverMessage || normalized.message;

        if (normalized.statusCode === 409) {
          setConflictNotice(
            'This request was changed by another user. Refreshing the latest state.',
          );
          await refreshRequest();
        } else if (normalized.statusCode === 403) {
          setActionError(
            `You are not authorized to perform "${descriptor.label}" on this request. The server rejected the request.`,
          );
          await refreshRequest();
        } else {
          setActionError(`${serverMessage} (${normalized.code})`);
        }
        // Re-throw so the action dialog can stay open with the reason attached.
        throw normalized;
      }
    },
    [requestId, refreshRequest],
  );

  const commentMutation = useMutation({
    mutationFn: (message: string) => omsRequestsService.addComment(requestId!, message),

    onSuccess: () => refreshRequest(),
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => omsRequestsService.uploadAttachment(requestId!, file),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ['oms-request-attachments', requestId] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (attachmentId: string) => omsRequestsService.deleteAttachment(attachmentId),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ['oms-request-attachments', requestId] }),
  });

  /**
   * Downloads through the authorized endpoint and hands the blob to the browser
   * as a temporary object URL. The object URL is revoked immediately after the
   * click so no long-lived reference to server content is retained.
   */
  const downloadMutation = useMutation({
    mutationFn: (attachment: OmsRequestAttachment) =>
      omsRequestsService.downloadAttachment(attachment.id),
    onSuccess: (blob, attachment) => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = attachment.fileName;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);

    },
  });

  const error = requestQuery.isError ? normalizeError(requestQuery.error) : null;

  if (requestQuery.isLoading) {
    return <LoadingSpinner message="Loading request..." />;
  }

  if (error) {
    const isNotFound = error.statusCode === 404;
    const isForbidden = error.statusCode === 403;
    return (
      <Box>
        <Button
          startIcon={<ArrowBackIcon />}
          onClick={() => navigate('/oms/requests')}
          sx={{ mb: 2 }}
        >
          Back to Requests
        </Button>
        <Alert
          severity={isNotFound || isForbidden ? 'warning' : 'error'}
          action={
            <Button color="inherit" size="small" onClick={() => requestQuery.refetch()}>
              Retry
            </Button>
          }
        >
          {isNotFound
            ? 'This request could not be found. It may have been deleted, or the identifier may be incorrect.'
            : isForbidden
              ? 'You do not have permission to view this request. The server has restricted it to the requester, the assignee, or an authorized queue role.'
              : `${error.serverMessage || error.message} (${error.code})`}
        </Alert>
      </Box>
    );
  }

  const request: OmsRequestDetail | undefined = requestQuery.data;
  if (!request) {
    return (
      <Alert
        severity="error"
        action={<Button onClick={() => requestQuery.refetch()}>Retry</Button>}
      >
        The request could not be loaded.
      </Alert>
    );
  }

  const actionContext = {
    status: request.status,
    userId: user?.id,
    requesterUserId: request.requesterUserId,
    assignedUserId: request.assignedUserId,
    roles: user?.roles,
  };

  const attachments = attachmentsQuery.data ?? [];
  const attachmentsError = attachmentsQuery.isError
    ? normalizeError(attachmentsQuery.error)
    : null;
  const frozen = isTerminalOmsRequestStatus(request.status);
  const relatedRoute = request.relatedEntityType
    ? RELATED_ENTITY_ROUTES[request.relatedEntityType.toLowerCase()]
    : undefined;
  const anyActionPending =
    commentMutation.isPending ||
    uploadMutation.isPending ||
    deleteMutation.isPending ||
    downloadMutation.isPending;

  return (
    <Box data-testid="request-detail">
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          justifyContent: 'space-between',
          alignItems: 'center',
          gap: 2,
          mb: 2,
        }}
      >
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/oms/requests')}>
          Back to Requests
        </Button>
        <Button
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={() => refreshRequest()}
          disabled={requestQuery.isFetching}
        >
          Refresh
        </Button>
      </Box>

      {conflictNotice && (
        <Alert severity="warning" sx={{ mb: 2 }} onClose={() => setConflictNotice(null)}>
          {conflictNotice}
        </Alert>
      )}
      {actionError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setActionError(null)}>
          {actionError}
        </Alert>
      )}

      <Paper sx={{ p: 3, mb: 2 }}>
        <Box
          sx={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: 1,
            alignItems: 'center',
            mb: 1,
          }}
        >
          <Typography variant="h5" fontWeight={600} sx={{ mr: 1 }}>
            {request.requestNumber}
          </Typography>
          <OmsRequestStatusChip status={request.status} />
          <OmsRequestPriorityChip priority={request.priority} />
          {frozen && (
            <Typography variant="caption" color="textSecondary">
              Terminal — this request is closed to further change.
            </Typography>
          )}
        </Box>
        <Typography variant="subtitle1" gutterBottom>
          {request.title}
        </Typography>
        <Typography variant="caption" color="textSecondary">
          {request.typeDisplayName} ({request.requestType})
        </Typography>

        <Divider sx={{ my: 2 }} />

        <Grid container spacing={2}>
          <DetailRow label="Request Number">{request.requestNumber}</DetailRow>
          <DetailRow label="Request Type">{request.typeDisplayName}</DetailRow>
          <DetailRow label="Status">{omsRequestStatusLabel(request.status)}</DetailRow>
          <DetailRow label="Requester">{request.requesterUserId}</DetailRow>
          <DetailRow label="Assignee">{request.assignedUserId || 'Unassigned'}</DetailRow>
          <DetailRow label="Priority">{request.priority}</DetailRow>
          <DetailRow label="Created">{formatDateTime(request.createdAt)}</DetailRow>
          <DetailRow label="Updated">{formatDateTime(request.updatedAt)}</DetailRow>
          <DetailRow label="Submitted">{formatDateTime(request.submittedAt)}</DetailRow>
          <DetailRow label="Due Date">{formatDateTime(request.dueDate)}</DetailRow>
          <DetailRow label="Completed">{formatDateTime(request.completedAt)}</DetailRow>
        </Grid>

        <Divider sx={{ my: 2 }} />

        <Typography variant="h6" fontWeight={600} gutterBottom>
          Description
        </Typography>
        {request.description ? (
          <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
            {request.description}
          </Typography>
        ) : (
          <Typography variant="body2" color="textSecondary">
            No description was provided.
          </Typography>
        )}

        {(request.relatedEntityType || request.relatedEntityId) && (
          <>
            <Divider sx={{ my: 2 }} />
            <Typography variant="h6" fontWeight={600} gutterBottom>
              Related Entity
            </Typography>
            <Typography variant="body2" color="textSecondary" display="block">
              {request.relatedEntityType || 'Unspecified type'}
              {request.relatedEntityId ? ` • ${request.relatedEntityId}` : ''}
            </Typography>
            {relatedRoute && request.relatedEntityId ? (
              <Button
                size="small"
                startIcon={<LinkIcon />}
                sx={{ mt: 1 }}
                onClick={() => navigate(relatedRoute.path(request.relatedEntityId!))}
              >
                {relatedRoute.label}
              </Button>
            ) : (
              <Typography variant="caption" color="textSecondary" sx={{ mt: 1 }} display="block">
                No direct link is available for this entity type.
              </Typography>
            )}
            <Typography variant="caption" color="textSecondary" sx={{ mt: 1 }} display="block">
              The owning module remains authoritative for this record. Opening a request does not
              change it.
            </Typography>
          </>
        )}
      </Paper>

      <Paper sx={{ p: 2.5, mb: 2 }}>
        <RequestActionsBar
          context={actionContext}
          submitting={anyActionPending}
          onAction={runLifecycleAction}
        />
        {frozen && (
          <Typography variant="caption" color="textSecondary" sx={{ mt: 1 }} display="block">
            No further lifecycle transitions are possible from{' '}
            {omsRequestStatusLabel(request.status)}.
          </Typography>
        )}
      </Paper>

      <Grid container spacing={2}>
        <Grid item xs={12} md={6}>
          <RequestStatusHistoryPanel
            entries={request.statusHistory ?? []}
            loading={requestQuery.isFetching}
          />
        </Grid>
        <Grid item xs={12} md={6}>
          <RequestCommentsPanel
            comments={request.comments ?? []}
            canComment={canCommentOnOmsRequest(user?.roles)}
            submitting={commentMutation.isPending}
            onAddComment={(message) => commentMutation.mutateAsync(message)}
          />
        </Grid>
        <Grid item xs={12}>
          {attachmentsQuery.isError ? (
            <Alert
              severity="warning"
              action={
                <Button color="inherit" size="small" onClick={() => attachmentsQuery.refetch()}>
                  Retry
                </Button>
              }
            >
              Attachments could not be loaded:{' '}
              {attachmentsError?.serverMessage || attachmentsError?.message}
            </Alert>
          ) : (
            <RequestAttachmentsPanel
              attachments={attachments}
              canUpload={canAttachToOmsRequest(actionContext)}
              canDelete={(attachment) => canDeleteOmsRequestAttachment(actionContext, attachment)}
              uploading={uploadMutation.isPending}
              frozen={frozen}
              onUpload={(file) => uploadMutation.mutateAsync(file)}
              onDownload={(attachment) => downloadMutation.mutateAsync(attachment)}
              onDelete={(attachment) => deleteMutation.mutateAsync(attachment.id)}
            />
          )}
        </Grid>
      </Grid>
    </Box>
  );
};

export default RequestDetailPage;
