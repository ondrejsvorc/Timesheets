import { ApiUrl, customFetch } from "@/constants/api";
import type { TimeRange, Timesheet } from "../../Timesheet";

type UpdateTimesheetResponse = { id: string };

type DayUpdate = {
  date: string;
  clockIn: string | null;
  clockOut: string | null;
  breakStart: string | null;
  breakEnd: string | null;
  description: string | null;
  schedules: TimeRange[] | null;
};

type ProjectDayUpdate = { date: string; hours: number };

type ProjectUpdate = {
  contractEmployeeId: string;
  lockedAt: string | null;
  lockedBy: string | null;
  days: ProjectDayUpdate[];
};

const toApiTime = (value: string): string | null => {
  if (!value) return null;
  return value.length === 5 ? `${value}:00` : value;
};

const dayDate = (year: number, month: number, day: number) => new Date(Date.UTC(year, month - 1, day)).toISOString();

const mapSchedules = (schedules: TimeRange[]): TimeRange[] | null => {
  const mapped = schedules
    .filter((range) => range.start || range.end)
    .map((range) => ({ start: toApiTime(range.start) ?? "", end: toApiTime(range.end) ?? "" }));
  return mapped.length > 0 ? mapped : null;
};

const buildRequest = (timesheet: Timesheet): { days: DayUpdate[]; projects: ProjectUpdate[] } => ({
  days: timesheet.days.map((day, index) => ({
    date: dayDate(timesheet.year, timesheet.month, index + 1),
    clockIn: toApiTime(day.attendance.clockIn),
    clockOut: toApiTime(day.attendance.clockOut),
    breakStart: toApiTime(day.attendance.breakStart),
    breakEnd: toApiTime(day.attendance.breakEnd),
    description: day.attendance.interruptions.trim() || null,
    schedules: mapSchedules(day.attendance.schedules),
  })),
  projects: timesheet.projects.map((project) => ({
    contractEmployeeId: project.id,
    lockedAt: project.lockedAt,
    lockedBy: project.lockedBy,
    days: timesheet.days.map((day, index) => ({
      date: dayDate(timesheet.year, timesheet.month, index + 1),
      hours: day.projectHours[project.id] ?? 0,
    })),
  })),
});

export const updateTimesheet = (timesheet: Timesheet, signal: AbortSignal) =>
  customFetch<UpdateTimesheetResponse>(`${ApiUrl}/timesheets/${timesheet.id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildRequest(timesheet)),
    signal,
  });
