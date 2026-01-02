// CombinedTimesheetTable.tsx
// ==========================

import { useState } from "react";
import { z } from "zod";
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
   ZOD VALIDATION
========================================================= */

const timeSchema = z.string().regex(/^\d{2}:\d{2}$/);

export const attendanceSchema = z
  .object({
    arrival: timeSchema.optional(),
    departure: timeSchema.optional(),
    breakStart: timeSchema.optional(),
    breakEnd: timeSchema.optional(),
  })
  .refine((v) => !v.arrival || !v.departure || v.departure >= v.arrival, { message: "Odchod nesmí být dříve než příchod" })
  .refine((v) => !v.breakStart || !v.arrival || v.breakStart >= v.arrival, { message: "Přestávka nesmí začít před příchodem" })
  .refine((v) => !v.breakEnd || !v.breakStart || v.breakEnd >= v.breakStart, { message: "Konec přestávky nesmí být před začátkem" });

/* =========================================================
   GENERATOR
========================================================= */

type ProjectInput = { id: string; workload: number };

export const generateTimesheet = (year: number, month: number, workload: number, projects: ProjectInput[]): CombinedTimesheetState => {
  const daysInMonth = new Date(year, month, 0).getDate();

  const days: CombinedDayState[] = [];

  for (let d = 1; d <= daysInMonth; d++) {
    const date = new Date(year, month - 1, d);
    const isWeekend = date.getDay() === 0 || date.getDay() === 6;

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
   UI CELLS
========================================================= */

const TimePickerCell = ({ value, onChange }: { value?: string; onChange: (v?: string) => void }) => (
  <Input type="time" value={value ?? ""} onChange={(e) => onChange(e.target.value || undefined)} className="w-28" />
);

const ComputedCell = ({ value }: { value: number }) => <span className="tabular-nums font-medium">{value.toFixed(2)}</span>;

const StagIntervalsCell = ({ intervals }: { intervals: TimeInterval[] }) => (
  <div className="flex items-center gap-2">
    <div className="flex flex-wrap gap-1">
      {intervals.map((i, idx) => (
        <Badge key={idx} variant="secondary">
          {i.from}–{i.to}
        </Badge>
      ))}
    </div>
    <Button size="icon" variant="ghost">
      ✏️
    </Button>
  </div>
);

/* =========================================================
   TABLE
========================================================= */

const initial = generateTimesheet(2026, 1, 1.0, [
  { id: "project-a", workload: 0.25 },
  { id: "project-b", workload: 0.5 },
]);

export const CombinedTimesheetTable = () => {
  const [state, setState] = useState(initial);

  return (
    <div className="relative max-h-[70vh] overflow-auto border rounded-md">
      <Table className="relative">
        <TableHeader>
          <TableRow>
            <TableHead className="sticky top-0 z-20 bg-background h-12">Datum</TableHead>
            <TableHead className="sticky top-0 z-20 bg-background h-12">Den</TableHead>
            <TableHead className="sticky top-0 z-20 bg-background h-12">Příchod</TableHead>
            <TableHead className="sticky top-0 z-20 bg-background h-12">Odchod</TableHead>
            <TableHead className="sticky top-0 z-20 bg-background h-12">Přestávka od</TableHead>
            <TableHead className="sticky top-0 z-20 bg-background h-12">Přestávka do</TableHead>
            <TableHead className="sticky top-0 z-20 bg-background h-12">Jiné přerušení</TableHead>
            <TableHead className="sticky top-0 z-20 bg-background h-12">Celkem (bez přestávky)</TableHead>
            <TableHead className="sticky top-0 z-20 bg-background h-12">Denní povinnost</TableHead>
            <TableHead className="sticky top-0 z-20 bg-background h-12">STAG</TableHead>
            <TableHead className="sticky top-0 z-20 bg-background h-12">Čas STAG</TableHead>
          </TableRow>
        </TableHeader>

        <TableBody>
          {state.days.map((day, idx) => {
            const worked = calculateWorkedHours(day.attendance.arrival, day.attendance.departure, day.attendance.breakStart, day.attendance.breakEnd);

            const stagHours = calculateIntervalsHours(day.attendance.stagIntervals);

            return (
              <TableRow key={day.date.toISOString()}>
                <TableCell>{day.date.toLocaleDateString("cs-CZ")}</TableCell>
                <TableCell>{getDayName(day.date)}</TableCell>

                <TableCell>
                  <TimePickerCell
                    value={day.attendance.arrival}
                    onChange={(v) =>
                      setState((s) => {
                        s = structuredClone(s);
                        s.days[idx].attendance.arrival = v;
                        return s;
                      })
                    }
                  />
                </TableCell>

                <TableCell>
                  <TimePickerCell
                    value={day.attendance.departure}
                    onChange={(v) =>
                      setState((s) => {
                        s = structuredClone(s);
                        s.days[idx].attendance.departure = v;
                        return s;
                      })
                    }
                  />
                </TableCell>

                <TableCell>
                  <TimePickerCell
                    value={day.attendance.breakStart}
                    onChange={(v) =>
                      setState((s) => {
                        s = structuredClone(s);
                        s.days[idx].attendance.breakStart = v;
                        return s;
                      })
                    }
                  />
                </TableCell>

                <TableCell>
                  <TimePickerCell
                    value={day.attendance.breakEnd}
                    onChange={(v) =>
                      setState((s) => {
                        s = structuredClone(s);
                        s.days[idx].attendance.breakEnd = v;
                        return s;
                      })
                    }
                  />
                </TableCell>

                <TableCell>
                  <Select>
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="Vyber" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="doctor">Lékař</SelectItem>
                      <SelectItem value="vacation">Dovolená</SelectItem>
                      <SelectItem value="other">Jiné</SelectItem>
                    </SelectContent>
                  </Select>
                </TableCell>

                <TableCell>
                  <ComputedCell value={worked} />
                </TableCell>

                <TableCell>
                  <ComputedCell value={day.attendance.dailyObligation} />
                </TableCell>

                <TableCell>
                  <StagIntervalsCell intervals={day.attendance.stagIntervals} />
                </TableCell>

                <TableCell>
                  <ComputedCell value={stagHours} />
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>

        <TableFooter>
          <TableRow>
            <TableCell colSpan={7} className="text-right font-semibold">
              Součet
            </TableCell>
            <TableCell colSpan={4} className="font-semibold">
              {state.days.reduce(
                (s, d) => s + calculateWorkedHours(d.attendance.arrival, d.attendance.departure, d.attendance.breakStart, d.attendance.breakEnd),
                0,
              )}
            </TableCell>
          </TableRow>
        </TableFooter>
      </Table>
    </div>
  );
};
