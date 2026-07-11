import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import type { TimesheetComment } from "./comments/Comment";
import type { ContractPartCell, TimeRange, Timesheet, TimesheetData, TimesheetEvaluation, TimesheetIssue } from "./Timesheet";

interface CompactContractPartDefinition {
  id: string;
  registrationNumber: string;
  name: string;
  position: string;
  workload: number;
  locked: boolean;
  activeDays: boolean[];
}

interface CompactDayItem {
  day: number;
  work: [number | null, number | null];
  break: [number | null, number | null];
  coreHours: number;
  contractPartCells: ContractPartCell[];
  isHoliday: boolean;
  isWeekend: boolean;
  note?: string | null;
  schedules?: Array<[number, number]> | null;
}

interface GetTimesheetResponse {
  id: string;
  year: number;
  month: number;
  coreWorkload: number;
  tracksAttendance: boolean;
  contractParts: CompactContractPartDefinition[];
  days: CompactDayItem[];
}

export interface TimesheetOverviewItem {
  timesheetId: string | null;
  kind: "core" | "contractPart";
  label: string;
  contractRegistrationNumber: string | null;
  position: string | null;
  workload: number;
  managers: string[];
  status: string;
  contractId: string | null;
  projectId: string | null;
}

export interface TimesheetMonthSummary {
  periodStart: string;
  periodEnd: string;
  workdays: number;
  vacationDays: number;
  sickDays: number;
  holidays: number;
  totalWorkload: number;
}

export interface GetTimesheetOverviewResponse {
  employeeId: string;
  year: number;
  month: number;
  status: string;
  items: TimesheetOverviewItem[];
  summary: TimesheetMonthSummary;
}

interface DraftDay {
  date: string;
  clockIn: string | null;
  clockOut: string | null;
  breakStart: string | null;
  breakEnd: string | null;
  coreHours: number;
  coreHoursFixed?: boolean;
  description: string | null;
  schedules: Array<{ start: string; end: string }> | null;
}

interface DraftContractPart {
  contractEmployeeId: string;
  days: Array<{ date: string; hours: number; hoursLocked?: boolean }>;
}

interface TimesheetDraft {
  days: DraftDay[];
  contractParts: DraftContractPart[];
}

interface ApiIssue {
  code: string;
  type: 0 | 1;
  description: string;
}

interface ApiDayIssue extends ApiIssue {
  day: number;
  field: string;
}

interface ApiTimesheetEvaluation extends Omit<TimesheetEvaluation, "issues"> {
  issues: ApiIssue[];
  dayIssues: ApiDayIssue[];
}

interface UpdateTimesheetResponse {
  id: string;
  evaluation: ApiTimesheetEvaluation;
}

interface ApiAllocationDay {
  work: [number | null, number | null];
  break: [number | null, number | null];
  coreHours: number;
  contractPartCells: Record<string, ContractPartCell>;
  attendanceAdjusted: boolean;
}

interface ApiAllocation {
  days: ApiAllocationDay[];
  evaluation: ApiTimesheetEvaluation;
}

export interface TimesheetAllocation {
  days: AllocationDay[];
  evaluation: TimesheetEvaluation;
}

interface AllocationDay {
  clockIn: string;
  clockOut: string;
  breakStart: string;
  breakEnd: string;
  coreHours: number;
  contractPartCells: Record<string, ContractPartCell>;
  attendanceAdjusted: boolean;
}

export interface TimesheetCommentAuthor {
  id: string;
  name: string;
}

export interface TimesheetStatusChangeDetails {
  changedBy: TimesheetCommentAuthor;
  timesheetLabel: string;
  fromStatus: string | null;
  toStatus: string;
  comment: string | null;
}

export interface TimesheetCommentItem {
  id: string;
  type: "message" | "statusChange";
  createdAt: string;
  text: string | null;
  author: TimesheetCommentAuthor | null;
  statusChange: TimesheetStatusChangeDetails | null;
}

export interface AddTimesheetCommentRequest {
  employeeId: string;
  year: number;
  month: number;
  text: string;
}

export interface DeleteTimesheetCommentRequest {
  commentId: string;
  employeeId: string;
  year: number;
  month: number;
}

export type TimesheetStatusAction = "submit" | "approve" | "return";

export interface UpdateTimesheetStatusRequest {
  employeeId: string;
  year: number;
  month: number;
  action: TimesheetStatusAction;
  comment?: string | null;
  timesheetIds: string[];
}

const pad2 = (value: number) => {
  return value.toString().padStart(2, "0");
};

const minutesToTime = (value: number | null | undefined) => {
  if (value == null || value < 0) return "";
  const hours = Math.floor(value / 60);
  const minutes = value % 60;
  return `${pad2(hours)}:${pad2(minutes)}`;
};

const minutesToDate = (year: number, month: number, day: number) => {
  return `${pad2(day)}. ${pad2(month)}. ${year}`;
};

const mapCompactSchedules = (schedules: Array<[number, number]> | null | undefined): TimeRange[] => {
  if (!schedules?.length) return [];
  return schedules.map(([start, end]) => ({
    start: minutesToTime(start),
    end: minutesToTime(end),
  }));
};

const mapToTimesheet = (response: GetTimesheetResponse): Timesheet => {
  const contractParts = response.contractParts.map((part) => ({
    id: part.id,
    registrationNumber: part.registrationNumber,
    name: part.name,
    position: part.position,
    workload: part.workload,
    locked: part.locked,
    activeDays: part.activeDays,
  }));

  const days = response.days.map((day) => {
    const contractPartCells = contractParts.reduce<Record<string, ContractPartCell>>((acc, part, index) => {
      acc[part.id] = day.contractPartCells[index] ?? { hours: 0, locked: false };
      return acc;
    }, {});

    return {
      date: minutesToDate(response.year, response.month, day.day),
      attendance: {
        clockIn: minutesToTime(day.work?.[0]),
        clockOut: minutesToTime(day.work?.[1]),
        breakStart: minutesToTime(day.break?.[0]),
        breakEnd: minutesToTime(day.break?.[1]),
        interruptions: day.note ?? "",
        schedules: mapCompactSchedules(day.schedules),
      },
      coreHours: day.coreHours > 0 ? day.coreHours : null,
      contractPartCells,
      isHoliday: day.isHoliday,
      isWeekend: day.isWeekend,
    };
  });

  return {
    id: response.id,
    year: response.year,
    month: response.month,
    tracksAttendance: response.tracksAttendance,
    core: { workload: response.coreWorkload },
    contractParts,
    days,
  };
};

const toApiTime = (value: string): string | null => {
  if (!value) return null;
  return value.length === 5 ? `${value}:00` : value;
};

const dayDate = (year: number, month: number, day: number): string => {
  return new Date(Date.UTC(year, month - 1, day)).toISOString();
};

const mapDraftSchedules = (schedules: Timesheet["days"][number]["attendance"]["schedules"]): DraftDay["schedules"] => {
  const complete = schedules
    .filter((range) => range.start && range.end)
    .map((range) => {
      return { start: toApiTime(range.start) ?? "", end: toApiTime(range.end) ?? "" };
    });
  return complete.length > 0 ? complete : null;
};

const mapIssue = (issue: ApiIssue | ApiDayIssue): TimesheetIssue => {
  return {
    code: issue.code,
    type: issue.type === 1 ? "error" : "warning",
    message: issue.description,
    ...("day" in issue ? { day: issue.day, field: issue.field } : {}),
  };
};

const buildTimesheetDraft = (timesheet: Timesheet): TimesheetDraft => {
  return {
    days: timesheet.days.map((day, index) => {
      return {
        date: dayDate(timesheet.year, timesheet.month, index + 1),
        clockIn: toApiTime(day.attendance.clockIn),
        clockOut: toApiTime(day.attendance.clockOut),
        breakStart: toApiTime(day.attendance.breakStart),
        breakEnd: toApiTime(day.attendance.breakEnd),
        coreHours: day.coreHours ?? 0,
        coreHoursFixed: day.coreHours !== null,
        description: day.attendance.interruptions.trim() || null,
        schedules: mapDraftSchedules(day.attendance.schedules),
      };
    }),
    contractParts: timesheet.contractParts.map((part) => {
      return {
        contractEmployeeId: part.id,
        days: timesheet.days.map((day, index) => {
          const active = part.activeDays[index] ?? true;
          const cell = day.contractPartCells[part.id] ?? { hours: 0, locked: false };
          return {
            date: dayDate(timesheet.year, timesheet.month, index + 1),
            hours: active ? cell.hours : 0,
            hoursLocked: active && cell.locked,
          };
        }),
      };
    }),
  };
};

const mapTimesheetEvaluation = (evaluation: ApiTimesheetEvaluation): TimesheetEvaluation => {
  return {
    hasErrors: evaluation.hasErrors,
    issues: [...evaluation.issues.map(mapIssue), ...evaluation.dayIssues.map(mapIssue)],
    days: evaluation.days,
    totals: evaluation.totals,
  };
};

const mapComment = (item: TimesheetCommentItem): TimesheetComment => {
  if (item.type === "statusChange") {
    if (!item.statusChange) {
      throw new Error("Status change comment is missing details.");
    }

    return {
      id: item.id,
      type: "statusChange",
      createdAt: item.createdAt,
      statusChange: item.statusChange,
    };
  }

  if (!item.author || !item.text) {
    throw new Error("Message comment is missing author or text.");
  }

  return {
    id: item.id,
    type: "message",
    createdAt: item.createdAt,
    text: item.text,
    author: {
      id: item.author.id,
      name: item.author.name,
    },
  };
};

export const getTimesheet = (employeeId: string, year: number, month: number): Promise<TimesheetData> => {
  const params = new URLSearchParams({
    employeeId,
    year: String(year),
    month: String(month),
  });

  return withDelay("slowest", async () => {
    const response = await customFetch<GetTimesheetResponse>(`${ApiUrl}/timesheets?${params.toString()}`);
    const timesheet = mapToTimesheet(response);
    return { timesheet, evaluation: await reviewTimesheet(timesheet) };
  });
};

export const getTimesheetOverview = (employeeId: string, year: number, month: number): Promise<GetTimesheetOverviewResponse> => {
  const params = new URLSearchParams({
    employeeId,
    year: String(year),
    month: String(month),
  });

  return withDelay("slow", () => {
    return customFetch<GetTimesheetOverviewResponse>(`${ApiUrl}/timesheets/overview?${params.toString()}`);
  });
};

export const updateTimesheet = async (timesheet: Timesheet, signal: AbortSignal): Promise<TimesheetEvaluation> => {
  const response = await customFetch<UpdateTimesheetResponse>(`${ApiUrl}/timesheets/${timesheet.id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildTimesheetDraft(timesheet)),
    signal,
  });
  return mapTimesheetEvaluation(response.evaluation);
};

export const reviewTimesheet = async (timesheet: Timesheet, signal?: AbortSignal): Promise<TimesheetEvaluation> => {
  const evaluation = await customFetch<ApiTimesheetEvaluation>(`${ApiUrl}/timesheets/${timesheet.id}/review`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildTimesheetDraft(timesheet)),
    signal,
  });
  return mapTimesheetEvaluation(evaluation);
};

export const allocateTimesheet = async (timesheet: Timesheet, day?: number): Promise<TimesheetAllocation> => {
  const query = day ? `?day=${day}` : "";
  const allocation = await customFetch<ApiAllocation>(`${ApiUrl}/timesheets/${timesheet.id}/allocate${query}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildTimesheetDraft(timesheet)),
  });
  return {
    days: allocation.days.map((day) => ({
      clockIn: minutesToTime(day.work?.[0]),
      clockOut: minutesToTime(day.work?.[1]),
      breakStart: minutesToTime(day.break?.[0]),
      breakEnd: minutesToTime(day.break?.[1]),
      coreHours: day.coreHours,
      contractPartCells: day.contractPartCells,
      attendanceAdjusted: day.attendanceAdjusted,
    })),
    evaluation: mapTimesheetEvaluation(allocation.evaluation),
  };
};

export const updateTimesheetStatus = async (request: UpdateTimesheetStatusRequest, signal?: AbortSignal): Promise<void> => {
  await customFetch<void>(`${ApiUrl}/timesheets/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      employeeId: request.employeeId,
      year: request.year,
      month: request.month,
      action: request.action,
      comment: request.comment?.trim() ? request.comment.trim() : null,
      timesheetIds: request.timesheetIds,
    }),
    signal,
  });
};

export const getTimesheetComments = (employeeId: string, year: number, month: number): Promise<TimesheetComment[]> => {
  const params = new URLSearchParams({
    employeeId,
    year: String(year),
    month: String(month),
  });

  return withDelay("fast", async () => {
    const response = await customFetch<TimesheetCommentItem[]>(`${ApiUrl}/timesheets/comments?${params.toString()}`);
    return response.map(mapComment);
  });
};

export const addTimesheetComment = async (request: AddTimesheetCommentRequest, signal?: AbortSignal): Promise<TimesheetComment> => {
  const response = await customFetch<TimesheetCommentItem>(`${ApiUrl}/timesheets/comments`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
    signal,
  });

  return mapComment(response);
};

export const deleteTimesheetComment = async (request: DeleteTimesheetCommentRequest, signal?: AbortSignal): Promise<void> => {
  const params = new URLSearchParams({
    employeeId: request.employeeId,
    year: String(request.year),
    month: String(request.month),
  });

  await customFetch<void>(`${ApiUrl}/timesheets/comments/${request.commentId}?${params.toString()}`, {
    method: "DELETE",
    signal,
  });
};
