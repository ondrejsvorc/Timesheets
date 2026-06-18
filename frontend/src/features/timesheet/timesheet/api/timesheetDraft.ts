import type { Timesheet, TimesheetEvaluation, TimesheetIssue } from "../../Timesheet";

type DraftDay = {
  date: string;
  clockIn: string | null;
  clockOut: string | null;
  breakStart: string | null;
  breakEnd: string | null;
  coreHours: number;
  coreHoursFixed?: boolean;
  description: string | null;
  schedules: Array<{ start: string; end: string }> | null;
};

type DraftProject = {
  contractEmployeeId: string;
  days: Array<{ date: string; hours: number; hoursFixed?: boolean }>;
};

export type TimesheetDraft = {
  days: DraftDay[];
  projects: DraftProject[];
};

type ApiIssue = {
  code: string;
  type: 0 | 1;
  description: string;
};

type ApiDayIssue = ApiIssue & {
  day: number;
  field: string;
};

export type ApiTimesheetEvaluation = Omit<TimesheetEvaluation, "issues"> & {
  issues: ApiIssue[];
  dayIssues: ApiDayIssue[];
};

const toApiTime = (value: string): string | null => {
  if (!value) return null;
  return value.length === 5 ? `${value}:00` : value;
};

const dayDate = (year: number, month: number, day: number): string => new Date(Date.UTC(year, month - 1, day)).toISOString();

const mapSchedules = (schedules: Timesheet["days"][number]["attendance"]["schedules"]): DraftDay["schedules"] => {
  const complete = schedules.filter((range) => range.start && range.end).map((range) => ({ start: toApiTime(range.start) ?? "", end: toApiTime(range.end) ?? "" }));
  return complete.length > 0 ? complete : null;
};

const mapIssue = (issue: ApiIssue | ApiDayIssue): TimesheetIssue => ({
  code: issue.code,
  type: issue.type === 1 ? "error" : "warning",
  message: issue.description,
  ...("day" in issue ? { day: issue.day, field: issue.field } : {}),
});

export const buildTimesheetDraft = (timesheet: Timesheet): TimesheetDraft => ({
  days: timesheet.days.map((day, index) => ({
    date: dayDate(timesheet.year, timesheet.month, index + 1),
    clockIn: toApiTime(day.attendance.clockIn),
    clockOut: toApiTime(day.attendance.clockOut),
    breakStart: toApiTime(day.attendance.breakStart),
    breakEnd: toApiTime(day.attendance.breakEnd),
    coreHours: day.coreHours ?? 0,
    coreHoursFixed: day.coreHours !== null,
    description: day.attendance.interruptions.trim() || null,
    schedules: mapSchedules(day.attendance.schedules),
  })),
  projects: timesheet.projects.map((project) => ({
    contractEmployeeId: project.id,
    days: timesheet.days.map((day, index) => {
      const active = project.activeDays[index] ?? true;
      const hours = active ? (day.projectHours[project.id] ?? 0) : 0;
      return {
        date: dayDate(timesheet.year, timesheet.month, index + 1),
        hours,
        hoursFixed: active && hours > 0,
      };
    }),
  })),
});

export const mapTimesheetEvaluation = (evaluation: ApiTimesheetEvaluation): TimesheetEvaluation => ({
  hasErrors: evaluation.hasErrors,
  issues: [...evaluation.issues.map(mapIssue), ...evaluation.dayIssues.map(mapIssue)],
  days: evaluation.days,
  totals: evaluation.totals,
});
