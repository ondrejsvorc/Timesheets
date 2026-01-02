// CombinedTimesheetTableTanStackPoC.tsx
// ======================================

import React, { useMemo, useState } from "react";
import {
  type ColumnDef,
  flexRender,
  getCoreRowModel,
  useReactTable,
} from "@tanstack/react-table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableFooter, TableHead, TableHeader, TableRow } from "@/components/ui/table";

/* =========================================================
   DOMAIN TYPES (UI STATE)
========================================================= */

export type TimeInterval = {
  from: string;
  to: string;
};

export type AttendanceDayState = {
  arrival?: string;
  departure?: string;
  breakStart?: string;
  breakEnd?: string;
  interruptions: string[];
  stagIntervals: TimeInterval[];
  dailyObligation: number;
};

export type ProjectDayState = {
  hours?: number;
  obligation: number;
};

export type CombinedDayState = {
  date: Date;
  attendance: AttendanceDayState;
  projects: Record<string, ProjectDayState>;
  core: ProjectDayState;
};

export type CombinedTimesheetState = {
  year: number;
  month: number;
  days: CombinedDayState[];
};

/* =========================================================
   CONSTANTS & HELPERS
========================================================= */

const STANDARD_WORKDAY_HOURS = 8;

const DAY_NAMES_CZ = ["Ne", "Po", "Út", "St", "Čt", "Pá", "So"] as const;

const toMinutes = (t: string) => {
  const [h, m] = t.split(":").map(Number);
  return h * 60 + m;
};

const getDayName = (date: Date) => DAY_NAMES_CZ[date.getDay()];

const calculateWorkedHours = (arrival?: string, departure?: string, breakStart?: string, breakEnd?: string): number => {
  if (!arrival || !departure) return 0;
  let minutes = toMinutes(departure) - toMinutes(arrival);
  if (breakStart && breakEnd) {
    minutes -= toMinutes(breakEnd) - toMinutes(breakStart);
  }
  return Math.max(0, minutes / 60);
};

const calculateIntervalsHours = (intervals: TimeInterval[]) =>
  intervals.reduce((s, i) => s + Math.max(0, (toMinutes(i.to) - toMinutes(i.from)) / 60), 0);

/* =========================================================
   GENERATOR
========================================================= */

type ProjectInput = { id: string; workload: number; name: string };

export const generateTimesheet = (year: number, month: number, workload: number, projects: ProjectInput[]): CombinedTimesheetState => {
  const daysInMonth = new Date(year, month, 0).getDate();

  const days: CombinedDayState[] = [];

  for (let d = 1; d <= daysInMonth; d++) {
    const date = new Date(year, month - 1, d);

    const projectWorkloadSum = projects.reduce((s, p) => s + p.workload, 0);
    const coreWorkload = Math.max(0, workload - projectWorkloadSum);

    days.push({
      date,
      attendance: {
        interruptions: [],
        stagIntervals: [],
        dailyObligation: STANDARD_WORKDAY_HOURS,
      },
      projects: Object.fromEntries(
        projects.map((p) => [
          p.id,
          {
            hours: undefined,
            obligation: p.workload * STANDARD_WORKDAY_HOURS,
          },
        ]),
      ),
      core: {
        hours: undefined,
        obligation: coreWorkload * STANDARD_WORKDAY_HOURS,
      },
    });
  }

  return { year, month, days };
};

/* =========================================================
   TABLE COLUMN DEFINITIONS
========================================================= */

type DayRow = CombinedDayState & { index: number };

const createColumns = (
  projects: ProjectInput[],
  onUpdate: (dayIndex: number, field: string, value: unknown) => void,
): ColumnDef<DayRow>[] => {
  const columns: ColumnDef<DayRow>[] = [
    {
      id: "date",
      header: "Datum",
      accessorFn: (row) => row.date,
      cell: ({ row }) => (
        <div className="tabular-nums whitespace-nowrap">
          {row.original.date.toLocaleDateString("cs-CZ", { day: "2-digit", month: "2-digit", year: "numeric" })} {getDayName(row.original.date)}
        </div>
      ),
      size: 160,
      meta: {
        sticky: true,
        stickyOffset: 0,
        sectionStart: true,
      },
    },
  ];

  // Pracovní docházka (Work attendance) group - includes STAG
  columns.push(
    {
      id: "attendance-group",
      header: "Pracovní docházka",
      columns: [
        {
          id: "arrival",
          header: "Příchod",
          meta: {
            sectionStart: true,
          },
          cell: ({ row }) => (
            <Input
              type="time"
              value={row.original.attendance.arrival ?? ""}
              onChange={(e) => onUpdate(row.original.index, "attendance.arrival", e.target.value || undefined)}
              className="w-full min-w-[120px]"
            />
          ),
          size: 140,
        },
        {
          id: "departure",
          header: "Odchod",
          cell: ({ row }) => (
            <Input
              type="time"
              value={row.original.attendance.departure ?? ""}
              onChange={(e) => onUpdate(row.original.index, "attendance.departure", e.target.value || undefined)}
              className="w-full min-w-[120px]"
            />
          ),
          size: 140,
        },
        {
          id: "breakStart",
          header: "Začátek přestávky",
          cell: ({ row }) => (
            <Input
              type="time"
              value={row.original.attendance.breakStart ?? ""}
              onChange={(e) => onUpdate(row.original.index, "attendance.breakStart", e.target.value || undefined)}
              className="w-full min-w-[120px]"
            />
          ),
          size: 160,
        },
        {
          id: "breakEnd",
          header: "Konec přestávky",
          cell: ({ row }) => (
            <Input
              type="time"
              value={row.original.attendance.breakEnd ?? ""}
              onChange={(e) => onUpdate(row.original.index, "attendance.breakEnd", e.target.value || undefined)}
              className="w-full min-w-[120px]"
            />
          ),
          size: 160,
        },
        {
          id: "interruptions",
          header: () => (
            <div className="flex flex-col">
              <span>Jiné přerušení</span>
            </div>
          ),
          cell: ({ row }) => (
            <Select
              value={row.original.attendance.interruptions[0] || ""}
              onValueChange={(v) => onUpdate(row.original.index, "attendance.interruptions", v ? [v] : [])}
            >
              <SelectTrigger className="w-full min-w-[160px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="doctor">Lékař</SelectItem>
                <SelectItem value="vacation">Dovolená</SelectItem>
                <SelectItem value="other">Jiné</SelectItem>
              </SelectContent>
            </Select>
          ),
          size: 180,
        },
        {
          id: "totalWithoutBreak",
          header: "Celkem (bez přestávky)",
          cell: ({ row }) => {
            const worked = calculateWorkedHours(
              row.original.attendance.arrival,
              row.original.attendance.departure,
              row.original.attendance.breakStart,
              row.original.attendance.breakEnd,
            );
            return <span className="tabular-nums font-medium whitespace-nowrap">{worked.toFixed(2)}</span>;
          },
          size: 180,
        },
        {
          id: "attendanceDailyObligation",
          header: "Denní povinnost",
          cell: ({ row }) => (
            <span className="tabular-nums font-medium whitespace-nowrap">{row.original.attendance.dailyObligation.toFixed(1)}</span>
          ),
          size: 150,
        },
        {
          id: "stagIntervals",
          header: () => (
            <div className="flex flex-col">
              <span>Rozmezí rozvrhové akce STAG</span>
            </div>
          ),
          cell: ({ row }) => (
            <div className="flex items-center gap-2 min-w-[200px]">
              <div className="flex flex-wrap gap-1">
                {row.original.attendance.stagIntervals.map((i, idx) => (
                  <Badge key={idx} variant="secondary">
                    {i.from}–{i.to}
                  </Badge>
                ))}
              </div>
              <Button size="icon" variant="ghost" className="shrink-0">
                ✏️
              </Button>
            </div>
          ),
          size: 250,
        },
        {
          id: "stagHours",
          header: "Čas STAG",
          cell: ({ row }) => {
            const stagHours = calculateIntervalsHours(row.original.attendance.stagIntervals);
            return <span className="tabular-nums font-medium whitespace-nowrap">{stagHours.toFixed(2)}</span>;
          },
          size: 130,
        },
      ],
    },
  );

  // Projektová činnost (Project activity) groups
  projects.forEach((project, projectIdx) => {
    const projectNum = projectIdx + 1;
    columns.push({
      id: `project-${project.id}-group`,
      header: `Projektová činnost ${projectNum}`,
      columns: [
        {
          id: `project-${project.id}-hours`,
          meta: {
            sectionStart: true,
          },
          header: () => (
            <div className="flex flex-col">
              <span>Počet hodin</span>
            </div>
          ),
          cell: ({ row }) => {
            const projectState = row.original.projects[project.id];
            return (
              <div className="flex flex-col gap-1 min-w-[180px]">
                <Input
                  type="number"
                  step="0.1"
                  value={projectState?.hours ?? ""}
                  onChange={(e) => onUpdate(row.original.index, `projects.${project.id}.hours`, e.target.value ? Number(e.target.value) : undefined)}
                  className="w-full"
                />
              </div>
            );
          },
          size: 200,
        },
        {
          id: `project-${project.id}-obligation`,
          header: "Denní povinnost",
          cell: ({ row }) => {
            const projectState = row.original.projects[project.id];
            return <span className="tabular-nums font-medium whitespace-nowrap">{(projectState?.obligation ?? 0).toFixed(1)}</span>;
          },
          size: 150,
        },
      ],
    });
  });

  // Kmenový úvazek (Core workload) group
  columns.push({
    id: "core-group",
    header: "Kmenový úvazek",
    columns: [
      {
        id: "core-hours",
        meta: {
          sectionStart: true,
        },
        header: "Počet hodin",
        cell: ({ row }) => {
          const coreHours = row.original.core.hours;
          return <span className="tabular-nums font-medium whitespace-nowrap">{coreHours !== undefined ? coreHours.toFixed(2) : "XX.XX"}</span>;
        },
        size: 150,
      },
      {
        id: "core-obligation",
        header: "Denní povinnost",
        cell: ({ row }) => <span className="tabular-nums font-medium whitespace-nowrap">{row.original.core.obligation.toFixed(1)}</span>,
        size: 150,
      },
    ],
  });

  // Kontrolní součty (Control totals) group
  columns.push({
    id: "control-totals-group",
    header: "Kontrolní součty",
    columns: [
      {
        id: "totalWorkedHours",
        meta: {
          sectionStart: true,
        },
        header: "Celkem odpracovaných hodin",
        cell: ({ row }) => {
          const worked = calculateWorkedHours(
            row.original.attendance.arrival,
            row.original.attendance.departure,
            row.original.attendance.breakStart,
            row.original.attendance.breakEnd,
          );
          return <span className="tabular-nums font-medium whitespace-nowrap">{worked.toFixed(2)}</span>;
        },
        size: 220,
      },
    ],
  });

  return columns;
};

/* =========================================================
   TABLE COMPONENT
========================================================= */

const initialProjects: ProjectInput[] = [
  { id: "project-a", workload: 0.25, name: "Project A" },
  { id: "project-b", workload: 0.5, name: "Project B" },
  { id: "project-c", workload: 0.25, name: "Project C" },
];

const initial = generateTimesheet(2025, 1, 1.0, initialProjects);

export const CombinedTimesheetTableTanStack = () => {
  const [state, setState] = useState(initial);
  const [projects] = useState(initialProjects);

  const updateDay = (dayIndex: number, field: string, value: unknown) => {
    setState((prev) => {
      const next = structuredClone(prev);
      const day = next.days[dayIndex];
      const parts = field.split(".");

      if (parts[0] === "attendance") {
        if (parts[1] === "interruptions") {
          day.attendance.interruptions = value as string[];
        } else {
          (day.attendance as Record<string, unknown>)[parts[1]] = value;
        }
      } else if (parts[0] === "projects") {
        const projectId = parts[1];
        const projectField = parts[2];
        if (!day.projects[projectId]) {
          day.projects[projectId] = { hours: undefined, obligation: 0 };
        }
        (day.projects[projectId] as Record<string, unknown>)[projectField] = value;
      } else if (parts[0] === "core") {
        (day.core as Record<string, unknown>)[parts[1]] = value;
      }

      return next;
    });
  };

  const data: DayRow[] = useMemo(
    () => state.days.map((day, idx) => ({ ...day, index: idx })),
    [state.days],
  );

  const projectList = useMemo(() => projects, [projects]);

  const columns: ColumnDef<DayRow>[] = useMemo(() => createColumns(projectList, updateDay), [projectList, updateDay]);

  const table = useReactTable({
    data: data as DayRow[],
    columns: columns as ColumnDef<DayRow>[],
    getCoreRowModel: getCoreRowModel(),
    getRowId: (row: DayRow) => row.index.toString(),
    columnResizeMode: "onChange",
  } as any);

  // Calculate totals for footer
  const totals = useMemo(() => {
    const totalWorked = state.days.reduce(
      (sum, day) => sum + calculateWorkedHours(day.attendance.arrival, day.attendance.departure, day.attendance.breakStart, day.attendance.breakEnd),
      0,
    );
    const totalStag = state.days.reduce((sum, day) => sum + calculateIntervalsHours(day.attendance.stagIntervals), 0);
    const totalProjects = Object.keys(state.days[0]?.projects || {}).map((projectId) =>
      state.days.reduce((sum, day) => sum + (day.projects[projectId]?.hours || 0), 0),
    );
    const totalCore = state.days.reduce((sum, day) => sum + (day.core.hours || 0), 0);

    return {
      totalWorked,
      totalStag,
      totalProjects,
      totalCore,
    };
  }, [state.days]);

  return (
    <div className="relative max-h-[70vh] overflow-auto border rounded-md p-4">
      <div className="overflow-x-auto">
        <Table className="border-collapse" style={{ width: table.getTotalSize() }}>
          <TableHeader className="sticky top-0 z-30 bg-background">
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header, headerIdx) => {
                  const meta = header.column.columnDef.meta as { sticky?: boolean; stickyOffset?: number; sectionStart?: boolean } | undefined;
                  const isSticky = meta?.sticky;
                  const stickyOffset = meta?.stickyOffset ?? 0;
                  const isSectionStart = meta?.sectionStart === true;
                  
                  // Show border if this is a section start and not the first column
                  const shouldShowBorder = isSectionStart && headerIdx > 0;

                  return (
                    <TableHead
                      key={header.id}
                      colSpan={header.colSpan}
                      className={`${isSticky ? "sticky bg-background z-20" : ""} overflow-visible ${shouldShowBorder ? "border-l-2 border-border" : ""}`}
                      style={{
                        left: isSticky ? `${stickyOffset}px` : undefined,
                        width: header.getSize(),
                        minWidth: header.getSize(),
                        maxWidth: header.getSize(),
                      }}
                    >
                      {header.isPlaceholder ? null : (
                        <div className="font-semibold whitespace-nowrap">
                          {flexRender(header.column.columnDef.header, header.getContext())}
                        </div>
                      )}
                    </TableHead>
                  );
                })}
              </TableRow>
            ))}
          </TableHeader>

          <TableBody>
            {table.getRowModel().rows.map((row) => (
              <TableRow key={row.id}>
                {row.getVisibleCells().map((cell, cellIdx) => {
                  const meta = cell.column.columnDef.meta as { sticky?: boolean; stickyOffset?: number; sectionStart?: boolean } | undefined;
                  const isSticky = meta?.sticky;
                  const stickyOffset = meta?.stickyOffset ?? 0;
                  const isSectionStart = meta?.sectionStart === true;
                  
                  // Show border if this is a section start and not the first column
                  const shouldShowBorder = isSectionStart && cellIdx > 0;

                  return (
                    <TableCell
                      key={cell.id}
                      className={`${isSticky ? "sticky bg-background z-10" : ""} overflow-visible ${shouldShowBorder ? "border-l-2 border-border" : ""}`}
                      style={{
                        left: isSticky ? `${stickyOffset}px` : undefined,
                        width: cell.column.getSize(),
                        minWidth: cell.column.getSize(),
                        maxWidth: cell.column.getSize(),
                      }}
                    >
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  );
                })}
              </TableRow>
            ))}
          </TableBody>

          <TableFooter className="sticky bottom-0 z-30 bg-background border-t-2">
            <TableRow>
              <TableCell className="text-right font-semibold sticky bg-background z-20 border-l-2 border-border" style={{ left: 0 }}>
                Kontrolní součty
              </TableCell>
              <TableCell colSpan={6}></TableCell>
              <TableCell className="font-semibold tabular-nums">{totals.totalWorked.toFixed(2)}</TableCell>
              <TableCell className="font-semibold tabular-nums">{state.days[0]?.attendance.dailyObligation.toFixed(1) || "XXX"}</TableCell>
              <TableCell></TableCell>
              <TableCell className="font-semibold tabular-nums">{totals.totalStag.toFixed(2)}</TableCell>
              {totals.totalProjects.map((total, idx) => (
                <React.Fragment key={idx}>
                  <TableCell className={`font-semibold tabular-nums ${idx === 0 ? "border-l-2 border-border" : ""}`}>{total.toFixed(2)}</TableCell>
                  <TableCell className="font-semibold tabular-nums">
                    {(state.days[0]?.projects[Object.keys(state.days[0]?.projects || {})[idx]]?.obligation || 0).toFixed(1)}
                  </TableCell>
                </React.Fragment>
              ))}
              <TableCell className="font-semibold tabular-nums border-l-2 border-border">{totals.totalCore.toFixed(2)}</TableCell>
              <TableCell className="font-semibold tabular-nums">{(state.days[0]?.core.obligation || 0).toFixed(1)}</TableCell>
              <TableCell className="font-semibold tabular-nums border-l-2 border-border">{totals.totalWorked.toFixed(2)}</TableCell>
            </TableRow>
            <TableRow>
              <TableCell className="text-right font-semibold sticky bg-background z-20 border-l-2 border-border" style={{ left: 0 }}>
                Rozdíl
              </TableCell>
              <TableCell colSpan={6}></TableCell>
              <TableCell className="font-semibold tabular-nums">0</TableCell>
              <TableCell></TableCell>
              <TableCell className="font-semibold tabular-nums">---</TableCell>
              {totals.totalProjects.map((total, idx) => {
                const diff = total - (state.days[0]?.projects[Object.keys(state.days[0]?.projects || {})[idx]]?.obligation || 0) * state.days.length;
                return (
                  <React.Fragment key={idx}>
                    <TableCell className={`font-semibold tabular-nums ${idx === 0 ? "border-l-2 border-border" : ""}`}>
                      {diff !== 0 ? (diff > 0 ? `+${diff.toFixed(1)}` : diff.toFixed(1)) : "0"}
                    </TableCell>
                    <TableCell></TableCell>
                  </React.Fragment>
                );
              })}
              <TableCell className="font-semibold tabular-nums border-l-2 border-border">
                {(() => {
                  const diff = totals.totalCore - (state.days[0]?.core.obligation || 0) * state.days.length;
                  return diff !== 0 ? (diff > 0 ? `+${diff.toFixed(1)}` : diff.toFixed(1)) : "0";
                })()}
              </TableCell>
              <TableCell></TableCell>
              <TableCell></TableCell>
            </TableRow>
          </TableFooter>
        </Table>
      </div>
    </div>
  );
};

