// TimesheetAuditTablePoC.tsx
// ===========================
// Auditní timesheet s editorem dne - lepší UX než velká interaktivní tabulka

import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router";
import { CheckCircle2, AlertTriangle, XCircle } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type {
  CombinedDayState,
  CombinedTimesheetState,
  TimeInterval,
  ProjectDayState,
} from "./CombinedTimesheetTableTanStackPoC";
import { generateTimesheet } from "./CombinedTimesheetTableTanStackPoC";

/* =========================================================
   TYPES & HELPERS
========================================================= */

type DayStatus = "ok" | "warning" | "error";

type DayAuditRow = {
  date: Date;
  dayName: string;
  status: DayStatus;
  worked: number;
  obligation: number;
  difference: number;
  projects: number;
  stag: number;
  dayData: CombinedDayState;
};

const DAY_NAMES_CZ = ["Ne", "Po", "Út", "St", "Čt", "Pá", "So"] as const;

const getDayName = (date: Date) => DAY_NAMES_CZ[date.getDay()];

const toMinutes = (t: string) => {
  const [h, m] = t.split(":").map(Number);
  return h * 60 + m;
};

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

const calculateProjectsHours = (projects: Record<string, ProjectDayState>): number => {
  return Object.values(projects).reduce((sum, p) => sum + (p.hours || 0), 0);
};

const calculateDayStatus = (day: CombinedDayState): DayStatus => {
  const worked = calculateWorkedHours(
    day.attendance.arrival,
    day.attendance.departure,
    day.attendance.breakStart,
    day.attendance.breakEnd,
  );
  const projects = calculateProjectsHours(day.projects);
  const stag = calculateIntervalsHours(day.attendance.stagIntervals);
  const difference = worked - day.attendance.dailyObligation;
  const isWeekend = day.date.getDay() === 0 || day.date.getDay() === 6;

  // Chyby
  if (difference !== 0) return "error";
  if (projects > worked) return "error";
  if (stag > worked) return "error";

  // Varování
  if (isWeekend && worked > 0) return "warning";

  return "ok";
};

const createAuditRows = (state: CombinedTimesheetState): DayAuditRow[] => {
  return state.days.map((day) => {
    const worked = calculateWorkedHours(
      day.attendance.arrival,
      day.attendance.departure,
      day.attendance.breakStart,
      day.attendance.breakEnd,
    );
    const projects = calculateProjectsHours(day.projects);
    const stag = calculateIntervalsHours(day.attendance.stagIntervals);
    const difference = worked - day.attendance.dailyObligation;
    const status = calculateDayStatus(day);

    return {
      date: day.date,
      dayName: getDayName(day.date),
      status,
      worked,
      obligation: day.attendance.dailyObligation,
      difference,
      projects,
      stag,
      dayData: day,
    };
  });
};

const getStatusIcon = (status: DayStatus) => {
  switch (status) {
    case "ok":
      return <CheckCircle2 className="h-4 w-4 text-green-600" />;
    case "warning":
      return <AlertTriangle className="h-4 w-4 text-yellow-600" />;
    case "error":
      return <XCircle className="h-4 w-4 text-red-600" />;
  }
};



/* =========================================================
   MAIN COMPONENT
========================================================= */

const initialProjects = [
  { id: "project-a", workload: 0.15, name: "Project A" },
  { id: "project-b", workload: 0.2, name: "Project B" },
  { id: "project-c", workload: 0.15, name: "Project C" },
  { id: "project-d", workload: 0.2, name: "Project D" },
  { id: "project-e", workload: 0.15, name: "Project E" },
  { id: "project-f", workload: 0.15, name: "Project F" },
];

const initial = generateTimesheet(2026, 1, 1.0, initialProjects);

// Simulace dat s konflikty pro demo
const mockData = structuredClone(initial);
mockData.days[0].attendance.arrival = "08:00";
mockData.days[0].attendance.departure = "16:00";
mockData.days[1].attendance.arrival = "08:00";
mockData.days[1].attendance.departure = "15:30";
mockData.days[1].projects["project-a"].hours = 4.5;
mockData.days[1].projects["project-b"].hours = 4.0;
mockData.days[2].attendance.stagIntervals = [
  { from: "09:00", to: "10:00" },
  { from: "13:00", to: "14:00" },
];

export const TimesheetAuditTable = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [state] = useState(mockData);
  const tableRef = useRef<HTMLTableElement>(null);

  const auditRows = useMemo(() => createAuditRows(state), [state]);

  // Scroll na řádek podle query parametru
  useEffect(() => {
    const scrollToIndex = searchParams.get("scrollTo");
    if (scrollToIndex !== null && tableRef.current) {
      const index = parseInt(scrollToIndex, 10);
      const rows = tableRef.current.querySelectorAll("tbody tr");
      if (rows[index]) {
        setTimeout(() => {
          rows[index].scrollIntoView({ behavior: "smooth", block: "center" });
        }, 100);
      }
    }
  }, [searchParams]);

  const summary = useMemo(() => {
    const totalWorked = auditRows.reduce((sum, r) => sum + r.worked, 0);
    const totalObligation = auditRows.reduce((sum, r) => sum + r.obligation, 0);
    const totalDifference = totalWorked - totalObligation;
    const totalStag = auditRows.reduce((sum, r) => sum + r.stag, 0);

    return {
      totalWorked,
      totalObligation,
      totalDifference,
      totalStag,
    };
  }, [auditRows]);

  const handleRowClick = (row: DayAuditRow) => {
    const date = row.date;
    const year = date.getFullYear();
    const month = date.getMonth() + 1;
    const day = date.getDate();
    navigate(`/timesheets/edit/${year}/${month}/${day}`);
  };

  const monthName = state.days[0]?.date.toLocaleDateString("cs-CZ", { month: "long", year: "numeric" });

  return (
    <div className="space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-bold">{monthName}</h1>
      </div>

      {/* Souhrn */}
      <Card>
        <CardHeader>
          <CardTitle>Souhrn</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div>
              <div className="text-muted-foreground">Odpracováno</div>
              <div className="text-lg font-semibold">{summary.totalWorked.toFixed(1)} h</div>
            </div>
            <div>
              <div className="text-muted-foreground">Povinnost</div>
              <div className="text-lg font-semibold">{summary.totalObligation.toFixed(1)} h</div>
            </div>
            <div>
              <div className="text-muted-foreground">Rozdíl</div>
              <div className={`text-lg font-semibold ${summary.totalDifference === 0 ? "text-green-600" : "text-red-600"}`}>
                {summary.totalDifference > 0 ? "+" : ""}
                {summary.totalDifference.toFixed(1)} h
              </div>
            </div>
            <div>
              <div className="text-muted-foreground">STAG</div>
              <div className="text-lg font-semibold">{summary.totalStag.toFixed(1)} h</div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Auditní tabulka */}
      <Card>
        <CardContent className="p-4">
          <Table ref={tableRef} className="w-full table-fixed">
            <TableHeader>
              <TableRow>
                <TableHead className="w-32">Datum</TableHead>
                <TableHead className="w-16">
                  <div className="flex justify-center items-center">Stav</div>
                </TableHead>
                <TableHead className="w-28 text-right">Odpracováno</TableHead>
                <TableHead className="w-28 text-right">Povinnost</TableHead>
                <TableHead className="w-28 text-right">Rozdíl</TableHead>
                <TableHead className="w-28 text-right">Projekty</TableHead>
                <TableHead className="w-28 text-right">STAG</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {auditRows.map((row) => {
                const hasError = row.status === "error";
                const hasWarning = row.status === "warning";

                return (
                  <TableRow
                    key={row.date.toISOString()}
                    className={`cursor-pointer hover:bg-muted/50 ${hasError ? "bg-red-50 dark:bg-red-950/20" : hasWarning ? "bg-yellow-50 dark:bg-yellow-950/20" : ""}`}
                    onClick={() => handleRowClick(row)}
                  >
                    <TableCell className="font-medium">
                      {row.date.toLocaleDateString("cs-CZ", { day: "2-digit", month: "2-digit" })} {row.dayName}
                    </TableCell>
                    <TableCell>
                      <div className="flex justify-center items-center">{getStatusIcon(row.status)}</div>
                    </TableCell>
                    <TableCell className="text-right tabular-nums">{row.worked.toFixed(1)}</TableCell>
                    <TableCell className="text-right tabular-nums">{row.obligation.toFixed(1)}</TableCell>
                    <TableCell
                      className={`text-right tabular-nums font-medium ${row.difference === 0 ? "" : "text-red-600"}`}
                    >
                      {row.difference > 0 ? "+" : ""}
                      {row.difference.toFixed(1)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">{row.projects.toFixed(1)}</TableCell>
                    <TableCell className="text-right tabular-nums">{row.stag.toFixed(1)}</TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
};

