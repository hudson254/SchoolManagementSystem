import React, { useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Collapse,
  Divider,
  Grid,
  IconButton,
  MenuItem,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  ExpandLess as ExpandLessIcon,
  ExpandMore as ExpandMoreIcon,
  FilterAltOff as FilterOffIcon,
  PictureAsPdf as PdfIcon,
  Search as SearchIcon,
  TableChart as ExcelIcon,
} from '@mui/icons-material';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { accommodationService } from '../services/accommodation.service';
import { semesterService } from '../services/semester.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { EmptyState } from '../components/Common/EmptyState';
import type {
  AccommodationHouseOccupancyReport,
  AccommodationReportBase,
  AccommodationReportKey,
  AccommodationReportParams,
  HouseOccupancyHistoryReport,
  HouseOccupancyReportRow,
  HouseUtilizationSummaryReport,
  OccupancyByPeriodHouseRow,
  OccupancyByPeriodReport,
  OccupancyHistoryReport,
  OccupancyHistoryReportRow,
  OccupancySummaryReport,
  OccupantAccommodationHistoryReport,
  OccupantCandidate,
  OccupantType,
  ReportPagination,
} from '../types/accommodation.types';

/** Roles allowed to read accommodation reports (mirrors the ReceptionistAccess policy). */
const STAFF_ROLES = ['Receptionist', 'Coordinator', 'Administrator', 'SystemAdministrator'];

const HOUSE_STATUSES = ['Vacant', 'Occupied', 'Reserved', 'Maintenance', 'Disabled', 'Unavailable'];

/**
 * Every report the page can render. `key` is the same stable identifier the
 * export endpoint accepts, so the preview and the download can never drift apart.
 */
type ReportKind = 'house' | 'history' | 'houseHistory' | 'period' | 'occupant' | 'utilization';

interface ReportDefinition {
  key: AccommodationReportKey;
  label: string;
  shortLabel: string;
  description: string;
  kind: ReportKind;
  laneFilter: boolean;
  houseFilter: boolean;
  houseFilterRequired: boolean;
  statusFilter: boolean;
  occupantTypeFilter: boolean;
  semesterFilter: boolean;
  periodFilter: boolean;
  searchFilter: boolean;
  paged: boolean;
  /** Occupant history needs a term (search mode) or a chosen occupant (detail mode). */
  needsOccupantInput: boolean;
  /** Column set used by the shared house table. */
  houseVariant?: 'current' | 'occupied' | 'empty';
}

const REPORT_DEFINITIONS: ReportDefinition[] = [
  {
    key: 'current-occupancy',
    label: 'Current House Occupancy',
    shortLabel: 'Current',
    description:
      'Every house with its live occupancy, capacity and current occupants. Expand a row to see who is in the house.',
    kind: 'house',
    laneFilter: true,
    houseFilter: true,
    houseFilterRequired: false,
    statusFilter: true,
    occupantTypeFilter: true,
    semesterFilter: true,
    periodFilter: false,
    searchFilter: true,
    paged: true,
    needsOccupantInput: false,
    houseVariant: 'current',
  },
  {
    key: 'occupied-houses',
    label: 'Occupied Houses',
    shortLabel: 'Occupied',
    description: 'Only the houses that currently have at least one occupant.',
    kind: 'house',
    laneFilter: true,
    houseFilter: false,
    houseFilterRequired: false,
    statusFilter: true,
    occupantTypeFilter: true,
    semesterFilter: true,
    periodFilter: false,
    searchFilter: true,
    paged: true,
    needsOccupantInput: false,
    houseVariant: 'occupied',
  },
  {
    key: 'empty-houses',
    label: 'Empty Houses',
    shortLabel: 'Empty',
    description:
      'Houses with no active occupant, including the last occupant on record and the date they left.',
    kind: 'house',
    laneFilter: true,
    houseFilter: false,
    houseFilterRequired: false,
    statusFilter: true,
    occupantTypeFilter: false,
    semesterFilter: false,
    periodFilter: false,
    searchFilter: true,
    paged: true,
    needsOccupantInput: false,
    houseVariant: 'empty',
  },
  {
    key: 'occupancy-history',
    label: 'Occupancy History',
    shortLabel: 'History',
    description:
      'Every occupancy record that overlaps the selected period. An open-ended stay (no move-out date) counts as still occupying.',
    kind: 'history',
    laneFilter: true,
    houseFilter: true,
    houseFilterRequired: false,
    statusFilter: false,
    occupantTypeFilter: true,
    semesterFilter: true,
    periodFilter: true,
    searchFilter: true,
    paged: true,
    needsOccupantInput: false,
  },
  {
    key: 'house-history',
    label: 'House Occupancy History',
    shortLabel: 'House history',
    description: 'The complete stay history of one house, oldest stay first.',
    kind: 'houseHistory',
    laneFilter: true,
    houseFilter: true,
    houseFilterRequired: true,
    statusFilter: false,
    occupantTypeFilter: false,
    semesterFilter: false,
    periodFilter: true,
    searchFilter: false,
    paged: true,
    needsOccupantInput: false,
  },
  {
    key: 'occupancy-by-period',
    label: 'Occupancy By Period',
    shortLabel: 'By period',
    description:
      'Totals for the selected period plus a per-house breakdown of who occupied it during that period.',
    kind: 'period',
    laneFilter: true,
    houseFilter: true,
    houseFilterRequired: false,
    statusFilter: true,
    occupantTypeFilter: true,
    semesterFilter: true,
    periodFilter: true,
    searchFilter: true,
    paged: true,
    needsOccupantInput: false,
  },
  {
    key: 'occupant-history',
    label: 'Occupant Accommodation History',
    shortLabel: 'Occupant',
    description:
      'Search an occupant by name, student number or staff number, then open their full stay history.',
    kind: 'occupant',
    laneFilter: false,
    houseFilter: false,
    houseFilterRequired: false,
    statusFilter: false,
    occupantTypeFilter: true,
    semesterFilter: false,
    periodFilter: false,
    searchFilter: true,
    paged: true,
    needsOccupantInput: true,
  },
  {
    key: 'utilization-summary',
    label: 'House Utilization Summary',
    shortLabel: 'Utilization',
    description:
      'Capacity, occupancy and utilization rates for the selected scope — the headline numbers for a one-page report.',
    kind: 'utilization',
    laneFilter: true,
    houseFilter: true,
    houseFilterRequired: false,
    statusFilter: true,
    occupantTypeFilter: false,
    semesterFilter: true,
    periodFilter: false,
    searchFilter: true,
    paged: false,
    needsOccupantInput: false,
  },
];

const REPORTS_BY_KEY: Record<string, ReportDefinition> = REPORT_DEFINITIONS.reduce(
  (acc, definition) => ({ ...acc, [definition.key]: definition }),
  {},
);

const fmtDate = (value?: string | null): string => {
  if (!value) return '—';
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleDateString();
};

const fmtDateTime = (value?: string | null): string => {
  if (!value) return '—';
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return '—';
  return `${parsed.toLocaleString(undefined, { timeZone: 'UTC' })} UTC`;
};

const fmtCount = (value?: number | null): string => (value ?? 0).toLocaleString();

const fmtPercentage = (value?: number | null): string => `${(value ?? 0).toFixed(1)}%`;

/** Converts "2026-01-05" date-input values into UTC ISO strings the API expects. */
const toDateParam = (value: string, endOfDay = false): string | undefined => {
  if (!value) return undefined;
  const parsed = new Date(`${value}T${endOfDay ? '23:59:59' : '00:00:00'}Z`);
  return Number.isNaN(parsed.getTime()) ? undefined : parsed.toISOString();
};

const occupantTypeLabel = (type?: OccupantType | string | null): string =>
  type === 'Lecturer' ? 'Staff' : type === 'Student' ? 'Student' : String(type ?? '—');

const statusChipColor = (status?: string | null): 'default' | 'primary' | 'secondary' | 'success' | 'info' | 'warning' | 'error' => {
  switch (status) {
    case 'Occupied':
      return 'primary';
    case 'Full':
      return 'error';
    case 'Empty':
    case 'Vacant':
      return 'success';
    case 'Reserved':
      return 'info';
    case 'Maintenance':
      return 'warning';
    case 'Disabled':
    case 'Unavailable':
      return 'error';
    default:
      return 'default';
  }
};

interface SummaryCardsProps {
  summary: OccupancySummaryReport;
  /** Extra headline values some reports return instead of house totals. */
  extra?: { label: string; value: string }[];
}

const SummaryCards: React.FC<SummaryCardsProps> = ({ summary, extra = [] }) => {
  const cards = [
    { label: 'Total houses', value: fmtCount(summary.totalHouses) },
    { label: 'Occupied houses', value: fmtCount(summary.occupiedHouses) },
    { label: 'Empty houses', value: fmtCount(summary.emptyHouses) },
    { label: 'Total capacity', value: fmtCount(summary.totalCapacity) },
    { label: 'Occupied spaces', value: fmtCount(summary.occupiedSpaces) },
    { label: 'Available spaces', value: fmtCount(summary.availableSpaces) },
    { label: 'At full capacity', value: fmtCount(summary.housesAtFullCapacity) },
    { label: 'With free space', value: fmtCount(summary.housesWithAvailableCapacity) },
    { label: 'Never occupied', value: fmtCount(summary.housesNeverOccupied) },
    { label: 'Utilization', value: fmtPercentage(summary.occupancyPercentage) },
    ...extra,
  ];

  return (
    <Grid container spacing={1.5} sx={{ mb: 2 }}>
      {cards.map((card) => (
        <Grid item xs={6} sm={4} md={2.4} key={card.label}>
          <Paper variant="outlined" sx={{ p: 1.5, height: '100%' }}>
            <Typography variant="caption" color="text.secondary" display="block">
              {card.label}
            </Typography>
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              {card.value}
            </Typography>
          </Paper>
        </Grid>
      ))}
    </Grid>
  );
};

interface ReportMetaBarProps {
  title: string;
  generatedAtUtc: string;
  generatedBy: string;
  appliedFilters: { label: string; value: string }[];
  onExportPdf: () => void;
  onExportExcel: () => void;
  exporting: 'PDF' | 'EXCEL' | null;
  exportDisabled: boolean;
}

const ReportMetaBar: React.FC<ReportMetaBarProps> = ({
  title,
  generatedAtUtc,
  generatedBy,
  appliedFilters,
  onExportPdf,
  onExportExcel,
  exporting,
  exportDisabled,
}) => (
  <Paper variant="outlined" sx={{ p: 1.5, mb: 2 }}>
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, alignItems: 'center' }}>
      <Typography variant="subtitle1" sx={{ fontWeight: 600, flexGrow: 1 }}>
        {title}
      </Typography>
      <Button
        size="small"
        variant="outlined"
        startIcon={exporting === 'PDF' ? <CircularProgress size={14} /> : <PdfIcon />}
        onClick={onExportPdf}
        disabled={exportDisabled || exporting !== null}
      >
        PDF
      </Button>
      <Button
        size="small"
        variant="outlined"
        startIcon={exporting === 'EXCEL' ? <CircularProgress size={14} /> : <ExcelIcon />}
        onClick={onExportExcel}
        disabled={exportDisabled || exporting !== null}
      >
        Excel
      </Button>
    </Box>
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75, mt: 1, alignItems: 'center' }}>
      <Typography variant="caption" color="text.secondary">
        Generated {fmtDateTime(generatedAtUtc)} by {generatedBy || 'system'}
      </Typography>
      {(appliedFilters || []).map((filter) => (
        <Chip
          key={`${filter.label}:${filter.value}`}
          size="small"
          variant="outlined"
          label={`${filter.label}: ${filter.value}`}
        />
      ))}
    </Box>
  </Paper>
);

const OccupantSubTable: React.FC<{ occupants: HouseOccupancyReportRow['currentOccupants'] }> = ({
  occupants,
}) => (
  <Table size="small" sx={{ my: 1 }}>
    <TableHead>
      <TableRow>
        <TableCell>Occupant</TableCell>
        <TableCell>Number</TableCell>
        <TableCell>Type</TableCell>
        <TableCell>Allocated</TableCell>
        <TableCell>Move-in</TableCell>
        <TableCell>Checked in</TableCell>
        <TableCell>Status</TableCell>
        <TableCell>Semester</TableCell>
      </TableRow>
    </TableHead>
    <TableBody>
      {(occupants || []).map((occupant, index) => (
        <TableRow key={occupant.assignmentId || `occupant-${index}`}>
          <TableCell>{occupant.occupantName}</TableCell>
          <TableCell>{occupant.occupantNumber || '—'}</TableCell>
          <TableCell>{occupantTypeLabel(occupant.occupantType)}</TableCell>
          <TableCell>{fmtDate(occupant.allocationDate)}</TableCell>
          <TableCell>{fmtDate(occupant.moveInDate)}</TableCell>
          <TableCell>{fmtDate(occupant.checkInDate)}</TableCell>
          <TableCell>{occupant.assignmentStatus}</TableCell>
          <TableCell>{occupant.semesterName || '—'}</TableCell>
        </TableRow>
      ))}
    </TableBody>
  </Table>
);

interface HouseOccupancyTableProps {
  rows: HouseOccupancyReportRow[];
  variant: 'current' | 'occupied' | 'empty';
  expandedHouseId: string | null;
  onToggle: (houseId: string) => void;
}

const HouseOccupancyTable: React.FC<HouseOccupancyTableProps> = ({
  rows,
  variant,
  expandedHouseId,
  onToggle,
}) => {
  const emptyView = variant === 'empty';
  return (
    <TableContainer sx={{ maxHeight: 620 }}>
      <Table size="small" stickyHeader>
        <TableHead>
          <TableRow>
            <TableCell width={44} />
            <TableCell>House</TableCell>
            <TableCell>Lane</TableCell>
            <TableCell align="right">Occupied</TableCell>
            <TableCell align="right">Capacity</TableCell>
            <TableCell align="right">Free</TableCell>
            <TableCell>Status</TableCell>
            {emptyView ? (
              <>
                <TableCell>Last occupant</TableCell>
                <TableCell>Left on</TableCell>
              </>
            ) : (
              <>
                <TableCell>Occupancy</TableCell>
                <TableCell align="right">Occupants</TableCell>
              </>
            )}
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row, index) => {
            const expanded = expandedHouseId === row.houseId;
            return (
              <React.Fragment key={row.houseId || `house-${index}`}>
                <TableRow hover onClick={() => onToggle(row.houseId)} sx={{ cursor: 'pointer' }}>
                  <TableCell padding="none">
                    <IconButton size="small" aria-label={expanded ? 'Collapse' : 'Expand'}>
                      {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                    </IconButton>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {row.houseNumber}
                    </Typography>
                    {row.houseName ? (
                      <Typography variant="caption" color="text.secondary" display="block">
                        {row.houseName}
                      </Typography>
                    ) : null}
                  </TableCell>
                  <TableCell>{row.laneName}</TableCell>
                  <TableCell align="right">{fmtCount(row.occupiedCount)}</TableCell>
                  <TableCell align="right">{fmtCount(row.capacity)}</TableCell>
                  <TableCell align="right">{fmtCount(row.availableSpaces)}</TableCell>
                  <TableCell>
                    <Chip size="small" label={row.status} color={statusChipColor(row.status)} />
                  </TableCell>
                  {emptyView ? (
                    <>
                      <TableCell>
                        {row.lastOccupantName ? (
                          <>
                            <Typography variant="body2">{row.lastOccupantName}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              {row.lastOccupantNumber || '—'} · {occupantTypeLabel(row.lastOccupantType)} ·{' '}
                              {fmtCount(row.historicalOccupantCount)} stay(s) on record
                            </Typography>
                          </>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            Never occupied
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>{fmtDate(row.lastOccupancyEndDate)}</TableCell>
                    </>
                  ) : (
                    <>
                      <TableCell>
                        <Chip
                          size="small"
                          variant="outlined"
                          label={row.occupancyStatus}
                          color={statusChipColor(row.occupancyStatus)}
                        />
                      </TableCell>
                      <TableCell align="right">
                        {fmtCount(row.currentOccupants?.length ?? 0)}
                      </TableCell>
                    </>
                  )}
                </TableRow>
                <TableRow>
                  <TableCell colSpan={8} sx={{ py: 0, borderBottom: expanded ? 1 : 0 }}>
                    <Collapse in={expanded} timeout="auto" unmountOnExit>
                      {emptyView ? (
                        <Typography variant="body2" color="text.secondary" sx={{ py: 1.5 }}>
                          {row.historicalOccupantCount > 0
                            ? `${fmtCount(row.historicalOccupantCount)} occupant(s) have stayed in this house. Vacant since ${fmtDate(
                                row.lastOccupancyEndDate,
                              )}.`
                            : 'This house has never been occupied.'}
                        </Typography>
                      ) : (row.currentOccupants?.length ?? 0) === 0 ? (
                        <Typography variant="body2" color="text.secondary" sx={{ py: 1.5 }}>
                          No active occupant.
                          {row.historicalOccupantCount > 0
                            ? ` ${fmtCount(row.historicalOccupantCount)} occupant(s) in the past.`
                            : ''}
                        </Typography>
                      ) : (
                        <OccupantSubTable occupants={row.currentOccupants || []} />
                      )}
                    </Collapse>
                  </TableCell>
                </TableRow>
              </React.Fragment>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
};

interface HistoryStaysTableProps {
  rows: OccupancyHistoryReportRow[];
  /** `occupancy` = all houses, `house` = one house (house columns dropped), `occupant` = one occupant. */
  variant: 'occupancy' | 'house' | 'occupant';
}

const HistoryStaysTable: React.FC<HistoryStaysTableProps> = ({ rows, variant }) => {
  const showHouse = variant !== 'house';
  const showOccupant = variant !== 'occupant';

  return (
    <TableContainer sx={{ maxHeight: 620 }}>
      <Table size="small" stickyHeader>
        <TableHead>
          <TableRow>
            {showHouse ? <TableCell>House</TableCell> : null}
            {showHouse ? <TableCell>Lane</TableCell> : null}
            {showOccupant ? <TableCell>Occupant</TableCell> : null}
            {showOccupant ? <TableCell>Number</TableCell> : null}
            {showOccupant ? <TableCell>Type</TableCell> : null}
            <TableCell>Occupancy from</TableCell>
            <TableCell>Occupancy to</TableCell>
            <TableCell align="right">Days</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Semester</TableCell>
            <TableCell>Academic year</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row, index) => (
            <TableRow key={`${row.assignmentId || 'stay'}-${row.occupantId || index}`} hover>
              {showHouse ? (
                <TableCell>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {row.houseNumber}
                  </Typography>
                  {row.houseName ? (
                    <Typography variant="caption" color="text.secondary" display="block">
                      {row.houseName}
                    </Typography>
                  ) : null}
                </TableCell>
              ) : null}
              {showHouse ? <TableCell>{row.laneName}</TableCell> : null}
              {showOccupant ? <TableCell>{row.occupantName}</TableCell> : null}
              {showOccupant ? <TableCell>{row.occupantNumber || '—'}</TableCell> : null}
              {showOccupant ? <TableCell>{occupantTypeLabel(row.occupantType)}</TableCell> : null}
              <TableCell>{fmtDate(row.occupancyStartDate)}</TableCell>
              <TableCell>
                {row.isCurrent ? (
                  <Chip size="small" color="success" label="Current" />
                ) : (
                  fmtDate(row.occupancyEndDate)
                )}
              </TableCell>
              <TableCell align="right">{fmtCount(row.durationDays)}</TableCell>
              <TableCell>
                <Chip size="small" variant="outlined" label={row.status} />
              </TableCell>
              <TableCell>{row.semesterName || '—'}</TableCell>
              <TableCell>{row.academicYearName || '—'}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};

const PeriodHouseTable: React.FC<{ rows: OccupancyByPeriodHouseRow[] }> = ({ rows }) => (
  <TableContainer sx={{ maxHeight: 620 }}>
    <Table size="small" stickyHeader>
      <TableHead>
        <TableRow>
          <TableCell>House</TableCell>
          <TableCell>Lane</TableCell>
          <TableCell>Status</TableCell>
          <TableCell align="right">Capacity</TableCell>
          <TableCell align="right">Occupied in period</TableCell>
          <TableCell align="right">Free in period</TableCell>
          <TableCell>Occupants in period</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {rows.map((row, index) => (
          <TableRow key={row.houseId || `period-${index}`} hover>
            <TableCell>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {row.houseNumber}
              </Typography>
              {row.houseName ? (
                <Typography variant="caption" color="text.secondary" display="block">
                  {row.houseName}
                </Typography>
              ) : null}
            </TableCell>
            <TableCell>{row.laneName}</TableCell>
            <TableCell>
              <Chip size="small" label={row.status} color={statusChipColor(row.status)} />
            </TableCell>
            <TableCell align="right">{fmtCount(row.capacity)}</TableCell>
            <TableCell align="right">{fmtCount(row.occupiedInPeriod)}</TableCell>
            <TableCell align="right">{fmtCount(row.availableInPeriod)}</TableCell>
            <TableCell>{row.occupantsInPeriod || '—'}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  </TableContainer>
);

interface OccupantSearchTableProps {
  candidates: OccupantCandidate[];
  onSelect: (candidate: OccupantCandidate) => void;
}

const OccupantSearchTable: React.FC<OccupantSearchTableProps> = ({ candidates, onSelect }) => (
  <TableContainer sx={{ maxHeight: 620 }}>
    <Table size="small" stickyHeader>
      <TableHead>
        <TableRow>
          <TableCell>Occupant</TableCell>
          <TableCell>Number</TableCell>
          <TableCell>Type</TableCell>
          <TableCell>Current house</TableCell>
          <TableCell align="right">Total stays</TableCell>
          <TableCell>Status</TableCell>
          <TableCell align="right">History</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {candidates.map((candidate, index) => (
          <TableRow
            key={`${candidate.occupantType || 'occupant'}-${candidate.occupantId || index}`}
            hover
          >
            <TableCell>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {candidate.occupantName}
              </Typography>
            </TableCell>
            <TableCell>{candidate.occupantNumber || '—'}</TableCell>
            <TableCell>{occupantTypeLabel(candidate.occupantType)}</TableCell>
            <TableCell>{candidate.currentHouseNumber || '—'}</TableCell>
            <TableCell align="right">{fmtCount(candidate.totalStays)}</TableCell>
            <TableCell>
              <Chip
                size="small"
                color={candidate.isCurrent ? 'success' : 'default'}
                label={candidate.isCurrent ? 'Currently housed' : 'Not housed'}
              />
            </TableCell>
            <TableCell align="right">
              <Button size="small" onClick={() => onSelect(candidate)}>
                View history
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  </TableContainer>
);

/** Reads one report page; keeps the switch next to the service so the two stay in sync. */
const fetchReport = (
  key: AccommodationReportKey,
  params: AccommodationReportParams,
): Promise<unknown> => {
  switch (key) {
    case 'current-occupancy':
      return accommodationService.getCurrentOccupancyReport(params);
    case 'occupied-houses':
      return accommodationService.getOccupiedHousesReport(params);
    case 'empty-houses':
      return accommodationService.getEmptyHousesReport(params);
    case 'occupancy-history':
      return accommodationService.getOccupancyHistoryReport(params);
    case 'house-history':
      return accommodationService.getHouseHistoryReport(String(params.houseId), params);
    case 'occupancy-by-period':
      return accommodationService.getOccupancyByPeriodReport(params);
    case 'occupant-history':
      return accommodationService.getOccupantHistoryReport(params);
    case 'utilization-summary':
      return accommodationService.getUtilizationSummaryReport(params);
    default:
      return Promise.reject(new Error(`Unknown report '${key}'`));
  }
};

interface ReportFilterState {
  laneId: string;
  houseId: string;
  status: string;
  occupantType: '' | OccupantType;
  semesterId: string;
  fromDate: string;
  toDate: string;
  searchTerm: string;
  occupantId: string | null;
  page: number;
  pageSize: number;
}

/**
 * Builds the query-string parameters for one report, dropping anything the
 * selected report does not understand so the API never sees stray filters.
 */
const buildReportParams = (
  definition: ReportDefinition,
  state: ReportFilterState,
  paged: boolean,
): AccommodationReportParams => {
  const params: AccommodationReportParams = {};
  const isOccupantReport = definition.kind === 'occupant';

  if (definition.laneFilter && state.laneId && !isOccupantReport) params.laneId = state.laneId;
  if (definition.houseFilter && state.houseId) params.houseId = state.houseId;
  if (definition.statusFilter && state.status) params.status = state.status;
  if (definition.occupantTypeFilter && state.occupantType) params.occupantType = state.occupantType;
  if (definition.semesterFilter && state.semesterId) params.semesterId = state.semesterId;

  if (definition.periodFilter) {
    const from = toDateParam(state.fromDate);
    const to = toDateParam(state.toDate, true);
    if (from) params.fromDate = from;
    if (to) params.toDate = to;
  }

  if (isOccupantReport && state.occupantId) {
    params.occupantId = state.occupantId;
  } else if (definition.searchFilter && state.searchTerm) {
    params.searchTerm = state.searchTerm;
  }

  if (definition.paged && paged) {
    params.page = state.page;
    params.pageSize = state.pageSize;
  }

  return params;
};

const downloadBlob = (blob: Blob, fileName: string): void => {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
};

const readErrorMessage = (error: unknown, fallback: string): string => {
  if (error && typeof error === 'object') {
    const candidate = error as { serverMessage?: string; message?: string };
    if (candidate.serverMessage) return candidate.serverMessage;
    if (candidate.message) return candidate.message;
  }
  return fallback;
};

export const AccommodationReports: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const canViewReports = useMemo(
    () => (user?.roles || []).some((role) => STAFF_ROLES.includes(role)),
    [user?.roles],
  );

  const [reportKey, setReportKey] = useState<AccommodationReportKey>('current-occupancy');
  const definition = REPORTS_BY_KEY[reportKey];

  const [laneId, setLaneId] = useState('');
  const [houseId, setHouseId] = useState('');
  const [status, setStatus] = useState('');
  const [occupantType, setOccupantType] = useState<'' | OccupantType>('');
  const [semesterId, setSemesterId] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [appliedSearch, setAppliedSearch] = useState('');
  const [selectedOccupant, setSelectedOccupant] = useState<OccupantCandidate | null>(null);

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [expandedHouseId, setExpandedHouseId] = useState<string | null>(null);
  const [exporting, setExporting] = useState<'PDF' | 'EXCEL' | null>(null);
  const [exportError, setExportError] = useState('');
  const [exportNotice, setExportNotice] = useState('');

  const { data: lanes } = useQuery({
    queryKey: ['lanes'],
    queryFn: () => accommodationService.getLanes(),
    enabled: canViewReports,
  });

  const { data: houses } = useQuery({
    queryKey: ['accommodation-report-houses', laneId],
    queryFn: () => accommodationService.getHouses(laneId || undefined),
    enabled: canViewReports && definition.houseFilter,
  });

  const { data: semesters } = useQuery({
    queryKey: ['semesters', { includeInactive: true }],
    queryFn: () => semesterService.getSemesters({ includeInactive: true }),
    enabled: canViewReports,
  });

  const filterState = useMemo<ReportFilterState>(
    () => ({
      laneId: definition.laneFilter ? laneId : '',
      houseId: definition.houseFilter ? houseId : '',
      status: definition.statusFilter ? status : '',
      occupantType: definition.occupantTypeFilter ? occupantType : '',
      semesterId: definition.semesterFilter ? semesterId : '',
      fromDate: definition.periodFilter ? fromDate : '',
      toDate: definition.periodFilter ? toDate : '',
      searchTerm: appliedSearch.trim(),
      occupantId: definition.kind === 'occupant' ? selectedOccupant?.occupantId ?? null : null,
      page,
      pageSize,
    }),
    [
      definition,
      laneId,
      houseId,
      status,
      occupantType,
      semesterId,
      fromDate,
      toDate,
      appliedSearch,
      selectedOccupant,
      page,
      pageSize,
    ],
  );

  const params = useMemo(() => buildReportParams(definition, filterState, true), [definition, filterState]);

  /** Exports always start at page 1 and are row-capped server side. */
  const exportParams = useMemo(
    () => buildReportParams(definition, filterState, false),
    [definition, filterState],
  );

  const missingRequirement =
    (definition.houseFilterRequired && !houseId) ||
    (definition.needsOccupantInput && !selectedOccupant && !appliedSearch.trim());

  const reportQuery = useQuery({
    queryKey: ['accommodation-report', reportKey, params],
    queryFn: () => fetchReport(reportKey, params),
    enabled: canViewReports && !missingRequirement,
    placeholderData: keepPreviousData,
  });

  const isRefreshing = reportQuery.isFetching && !reportQuery.isPlaceholderData;

  const houseReport =
    definition.kind === 'house'
      ? (reportQuery.data as AccommodationHouseOccupancyReport | undefined)
      : undefined;
  const historyReport =
    definition.kind === 'history' ? (reportQuery.data as OccupancyHistoryReport | undefined) : undefined;
  const houseHistoryReport =
    definition.kind === 'houseHistory'
      ? (reportQuery.data as HouseOccupancyHistoryReport | undefined)
      : undefined;
  const periodReport =
    definition.kind === 'period' ? (reportQuery.data as OccupancyByPeriodReport | undefined) : undefined;
  const occupantReport =
    definition.kind === 'occupant'
      ? (reportQuery.data as OccupantAccommodationHistoryReport | undefined)
      : undefined;
  const utilizationReport =
    definition.kind === 'utilization'
      ? (reportQuery.data as HouseUtilizationSummaryReport | undefined)
      : undefined;

  const summary: OccupancySummaryReport | undefined =
    houseReport?.summary ?? periodReport?.summary ?? utilizationReport?.summary;

  const reportBase = reportQuery.data as AccommodationReportBase | undefined;

  const pagination: ReportPagination | undefined = definition.paged
    ? houseReport?.pagination ??
      historyReport?.pagination ??
      houseHistoryReport?.pagination ??
      periodReport?.pagination ??
      occupantReport?.pagination
    : undefined;

  const changeReport = (next: AccommodationReportKey) => {
    if (next === reportKey) return;
    setReportKey(next);
    setPage(1);
    setExpandedHouseId(null);
    setSelectedOccupant(null);
    setExportError('');
    setExportNotice('');
  };

  const applyFilters = () => {
    setAppliedSearch(searchInput.trim());
    setPage(1);
    setExpandedHouseId(null);
    setExportError('');
    setExportNotice('');
  };

  const resetFilters = () => {
    setLaneId('');
    setHouseId('');
    setStatus('');
    setOccupantType('');
    setSemesterId('');
    setFromDate('');
    setToDate('');
    setSearchInput('');
    setAppliedSearch('');
    setSelectedOccupant(null);
    setPage(1);
    setExpandedHouseId(null);
    setExportError('');
    setExportNotice('');
  };

  const openOccupantHistory = (candidate: OccupantCandidate) => {
    setOccupantType(candidate.occupantType);
    setSelectedOccupant(candidate);
    setPage(1);
  };

  const handleExport = async (format: 'PDF' | 'EXCEL') => {
    setExporting(format);
    setExportError('');
    setExportNotice('');
    try {
      const blob = await accommodationService.exportAccommodationReport(
        reportKey,
        format,
        exportParams,
      );
      const extension = format === 'PDF' ? 'pdf' : 'xlsx';
      const stamp = new Date().toISOString().slice(0, 10);
      downloadBlob(blob, `Accommodation_${reportKey.replace(/-/g, '_')}_${stamp}.${extension}`);
      setExportNotice(
        `${definition.label} downloaded as ${format === 'PDF' ? 'PDF' : 'Excel workbook'}.`,
      );
    } catch (error) {
      setExportError(
        readErrorMessage(error, 'Unable to export this report. Please try again.'),
      );
    } finally {
      setExporting(null);
    }
  };

  const exportDisabled = missingRequirement || (!reportQuery.isSuccess && !reportQuery.isError);

  if (!canViewReports) {
    return (
      <Alert severity="warning" sx={{ m: 3 }}>
        Accommodation reports are available to reception and administration staff only.
      </Alert>
    );
  }

  return (
    <Box sx={{ p: { xs: 2, md: 3 } }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1, flexWrap: 'wrap' }}>
        <Button
          size="small"
          variant="text"
          startIcon={<ArrowBackIcon />}
          onClick={() => navigate('/accommodation')}
        >
          Accommodation
        </Button>
        <Typography variant="h4" sx={{ fontWeight: 700, flexGrow: 1 }}>
          Accommodation Reports
        </Typography>
        {isRefreshing ? <CircularProgress size={18} /> : null}
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Occupancy, history and utilization reports for student and staff housing. Every report can be
        downloaded as PDF or Excel with exactly the filters shown below.
      </Typography>

      <Paper variant="outlined" sx={{ p: 1.5, mb: 2 }}>
        <ToggleButtonGroup
          value={reportKey}
          exclusive
          size="small"
          onChange={(_event, next: AccommodationReportKey | null) => {
            if (next) changeReport(next);
          }}
          sx={{ flexWrap: 'wrap' }}
        >
          {REPORT_DEFINITIONS.map((item) => (
            <ToggleButton key={item.key} value={item.key} sx={{ textTransform: 'none' }}>
              {item.shortLabel}
            </ToggleButton>
          ))}
        </ToggleButtonGroup>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          {definition.description}
        </Typography>
      </Paper>
      <Paper variant="outlined" sx={{ p: 1.5, mb: 2 }}>
        <Grid container spacing={1.5} alignItems="flex-end">
          {definition.laneFilter && definition.kind !== 'occupant' ? (
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                select
                size="small"
                fullWidth
                label="Lane"
                value={laneId}
                onChange={(event) => {
                  setLaneId(event.target.value);
                  setHouseId('');
                  setPage(1);
                }}
              >
                <MenuItem value="">All lanes</MenuItem>
                {(lanes || []).map((lane) => (
                  <MenuItem key={lane.id} value={lane.id}>
                    {lane.laneName}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          ) : null}

          {definition.houseFilter ? (
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                select
                size="small"
                fullWidth
                required={definition.houseFilterRequired}
                label={definition.houseFilterRequired ? 'House (required)' : 'House'}
                value={houseId}
                onChange={(event) => {
                  setHouseId(event.target.value);
                  setPage(1);
                }}
              >
                <MenuItem value="">All houses</MenuItem>
                {(houses || []).map((house) => (
                  <MenuItem key={house.id} value={house.id}>
                    {house.houseNumber}
                    {house.houseName ? ` — ${house.houseName}` : ''} · {house.laneName}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          ) : null}

          {definition.statusFilter ? (
            <Grid item xs={6} sm={4} md={2}>
              <TextField
                select
                size="small"
                fullWidth
                label="House status"
                value={status}
                onChange={(event) => {
                  setStatus(event.target.value);
                  setPage(1);
                }}
              >
                <MenuItem value="">Any status</MenuItem>
                {HOUSE_STATUSES.map((option) => (
                  <MenuItem key={option} value={option}>
                    {option}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          ) : null}

          {definition.occupantTypeFilter ? (
            <Grid item xs={6} sm={4} md={2}>
              <TextField
                select
                size="small"
                fullWidth
                label="Occupant type"
                value={occupantType}
                onChange={(event) => {
                  setOccupantType(event.target.value as '' | OccupantType);
                  setSelectedOccupant(null);
                  setPage(1);
                }}
              >
                <MenuItem value="">All occupants</MenuItem>
                <MenuItem value="Student">Students</MenuItem>
                <MenuItem value="Lecturer">Staff</MenuItem>
              </TextField>
            </Grid>
          ) : null}
          {definition.semesterFilter ? (
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                select
                size="small"
                fullWidth
                label="Semester"
                value={semesterId}
                onChange={(event) => {
                  setSemesterId(event.target.value);
                  setPage(1);
                }}
              >
                <MenuItem value="">All semesters</MenuItem>
                {(semesters || []).map((semester) => (
                  <MenuItem key={semester.id} value={semester.id}>
                    {semester.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          ) : null}

          {definition.periodFilter ? (
            <Grid item container spacing={1.5}>
              <Grid item xs={6} sm={4} md={2}>
                <TextField
                  size="small"
                  fullWidth
                  type="date"
                  label="From"
                  InputLabelProps={{ shrink: true }}
                  value={fromDate}
                  onChange={(event) => {
                    setFromDate(event.target.value);
                    setPage(1);
                  }}
                />
              </Grid>
              <Grid item xs={6} sm={4} md={2}>
                <TextField
                  size="small"
                  fullWidth
                  type="date"
                  label="To"
                  InputLabelProps={{ shrink: true }}
                  value={toDate}
                  onChange={(event) => {
                    setToDate(event.target.value);
                    setPage(1);
                  }}
                />
              </Grid>
            </Grid>
          ) : null}

          {definition.searchFilter ? (
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                size="small"
                fullWidth
                label={
                  definition.kind === 'occupant'
                    ? 'Name, student or staff number'
                    : 'Occupant, house or lane'
                }
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter') applyFilters();
                }}
              />
            </Grid>
          ) : null}

          <Grid item xs={12} sm={6} md={3}>
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button
                variant="contained"
                size="small"
                startIcon={<SearchIcon />}
                onClick={applyFilters}
                disabled={isRefreshing}
              >
                Apply
              </Button>
              <Button
                size="small"
                variant="outlined"
                startIcon={<FilterOffIcon />}
                onClick={resetFilters}
              >
                Reset
              </Button>
            </Box>
          </Grid>
        </Grid>

        {definition.needsOccupantInput && !selectedOccupant && !appliedSearch.trim() ? (
          <Alert severity="info" sx={{ mt: 1.5 }}>
            Type a name, student number or staff number, then press Apply to list matching occupants.
          </Alert>
        ) : null}
        {definition.houseFilterRequired && !houseId ? (
          <Alert severity="info" sx={{ mt: 1.5 }}>
            Choose a house to load its stay-by-stay occupancy history.
          </Alert>
        ) : null}

        {exportError ? (
          <Alert severity="error" sx={{ mt: 1.5 }} onClose={() => setExportError('')}>
            {exportError}
          </Alert>
        ) : null}
        {exportNotice ? (
          <Alert severity="success" sx={{ mt: 1.5 }} onClose={() => setExportNotice('')}>
            {exportNotice}
          </Alert>
        ) : null}
      </Paper>
      {reportBase ? (
        <ReportMetaBar
          title={reportBase.reportTitle || definition.label}
          generatedAtUtc={reportBase.generatedAtUtc}
          generatedBy={reportBase.generatedBy}
          appliedFilters={reportBase.appliedFilters || []}
          onExportPdf={() => handleExport('PDF')}
          onExportExcel={() => handleExport('EXCEL')}
          exporting={exporting}
          exportDisabled={exportDisabled}
        />
      ) : null}

      {definition.kind === 'occupant' && selectedOccupant ? (
        <Paper variant="outlined" sx={{ p: 1.5, mb: 2 }}>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, alignItems: 'center' }}>
            <Button
              size="small"
              variant="text"
              startIcon={<ArrowBackIcon />}
              onClick={() => {
                setSelectedOccupant(null);
                setPage(1);
              }}
            >
              All occupants
            </Button>
            <Divider orientation="vertical" flexItem />
            <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
              {selectedOccupant.occupantName}
            </Typography>
            <Chip size="small" variant="outlined" label={selectedOccupant.occupantNumber || '—'} />
            <Chip
              size="small"
              variant="outlined"
              label={occupantTypeLabel(selectedOccupant.occupantType)}
            />
            <Chip
              size="small"
              color={selectedOccupant.isCurrent ? 'success' : 'default'}
              label={
                selectedOccupant.isCurrent
                  ? `Currently in ${selectedOccupant.currentHouseNumber}`
                  : 'Not currently housed'
              }
            />
          </Box>
        </Paper>
      ) : null}

      {reportQuery.isError && !reportQuery.isPlaceholderData ? (
        <Alert severity="error" sx={{ mb: 2 }}>
          {readErrorMessage(reportQuery.error, 'Unable to load this report. Please try again.')}
        </Alert>
      ) : null}

      {reportQuery.isLoading && !missingRequirement ? (
        <LoadingSpinner message="Generating report…" />
      ) : null}

      {missingRequirement || reportQuery.isLoading || reportQuery.isError ? null : (
        <>
          {summary ? <SummaryCards summary={summary} /> : null}

          {historyReport ? (
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
              <Chip
                size="small"
                variant="outlined"
                label={`Period: ${fmtDate(historyReport.periodStart)} → ${
                  historyReport.periodEnd ? fmtDate(historyReport.periodEnd) : 'today'
                }`}
              />
              <Chip
                size="small"
                variant="outlined"
                label={`Distinct occupants: ${fmtCount(historyReport.distinctOccupants)}`}
              />
              <Chip
                size="small"
                variant="outlined"
                label={`Houses used: ${fmtCount(historyReport.distinctHouses)}`}
              />
            </Box>
          ) : null}

          {houseHistoryReport ? (
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
              <Chip
                size="small"
                variant="outlined"
                label={`House: ${houseHistoryReport.houseNumber}${
                  houseHistoryReport.houseName ? ` — ${houseHistoryReport.houseName}` : ''
                }`}
              />
              <Chip size="small" variant="outlined" label={`Lane: ${houseHistoryReport.laneName}`} />
              <Chip
                size="small"
                variant="outlined"
                label={`Status: ${houseHistoryReport.houseStatus}`}
              />
              <Chip
                size="small"
                variant="outlined"
                label={`Capacity: ${fmtCount(houseHistoryReport.capacity)}`}
              />
              <Chip
                size="small"
                variant="outlined"
                label={`Current occupants: ${fmtCount(houseHistoryReport.currentOccupants)}`}
              />
              <Chip
                size="small"
                variant="outlined"
                label={`Total stays: ${fmtCount(houseHistoryReport.totalStays)}`}
              />
            </Box>
          ) : null}

          {occupantReport && occupantReport.mode !== 'Search' ? (
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
              <Chip
                size="small"
                variant="outlined"
                label={`Current house: ${occupantReport.currentHouse || '—'}`}
              />
              <Chip
                size="small"
                variant="outlined"
                label={`Stays: ${fmtCount(occupantReport.stays?.length)}`}
              />
            </Box>
          ) : null}
          {definition.kind === 'house' && houseReport ? (
            (houseReport.rows?.length ?? 0) > 0 ? (
              <HouseOccupancyTable
                rows={houseReport.rows}
                variant={definition.houseVariant ?? 'current'}
                expandedHouseId={expandedHouseId}
                onToggle={(houseRowId) =>
                  setExpandedHouseId((previous) => (previous === houseRowId ? null : houseRowId))
                }
              />
            ) : (
              <EmptyState
                title="No house matches these filters"
                description="Widen the lane, status or search filters, then apply them again."
              />
            )
          ) : null}

          {definition.kind === 'history' && historyReport ? (
            (historyReport.rows?.length ?? 0) > 0 ? (
              <HistoryStaysTable rows={historyReport.rows} variant="occupancy" />
            ) : (
              <EmptyState
                title="No occupancy in this period"
                description="No stay overlapped the selected period, lane, house or occupant type."
              />
            )
          ) : null}

          {definition.kind === 'houseHistory' && houseHistoryReport ? (
            (houseHistoryReport.rows?.length ?? 0) > 0 ? (
              <HistoryStaysTable rows={houseHistoryReport.rows} variant="house" />
            ) : (
              <EmptyState
                title="This house has never been occupied"
                description="No stay has been recorded for the selected house."
              />
            )
          ) : null}

          {definition.kind === 'period' && periodReport ? (
            (periodReport.rows?.length ?? 0) > 0 ? (
              <PeriodHouseTable rows={periodReport.rows} />
            ) : (
              <EmptyState
                title="Nothing occupied in this period"
                description="No house recorded an occupant inside the selected period."
              />
            )
          ) : null}

          {definition.kind === 'occupant' && occupantReport ? (
            occupantReport.mode !== 'Search' ? (
              (occupantReport.stays?.length ?? 0) > 0 ? (
                <HistoryStaysTable rows={occupantReport.stays} variant="occupant" />
              ) : (
                <EmptyState
                  title="No stay recorded"
                  description="This occupant has never been allocated a house."
                />
              )
            ) : (occupantReport.candidates?.length ?? 0) > 0 ? (
              <OccupantSearchTable
                candidates={occupantReport.candidates}
                onSelect={openOccupantHistory}
              />
            ) : (
              <EmptyState
                title="No matching occupant"
                description="Search by full name, student number or staff number — partial values are enough."
              />
            )
          ) : null}

          {definition.kind === 'utilization' && utilizationReport ? (
            <Typography variant="body2" color="text.secondary">
              These totals cover {fmtCount(utilizationReport.summary.totalHouses)} house(s) in the
              selected scope. Export the report for a printable one-page summary.
            </Typography>
          ) : null}

          {definition.paged && pagination ? (
            <TablePagination
              component="div"
              count={pagination.totalCount}
              page={Math.max(0, (pagination.page || page) - 1)}
              onPageChange={(_event, nextPage) => {
                setPage(nextPage + 1);
                setExpandedHouseId(null);
              }}
              rowsPerPage={pagination.pageSize || pageSize}
              onRowsPerPageChange={(event) => {
                setPageSize(parseInt(event.target.value, 10) || 50);
                setPage(1);
              }}
              rowsPerPageOptions={[25, 50, 100, 250]}
            />
          ) : null}
        </>
      )}
    </Box>
  );
};
