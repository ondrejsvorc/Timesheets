import { ApiUrl, customFetch } from "@/constants/api";
import type { Timesheet, TimesheetEvaluation } from "../../Timesheet";
import { type ApiTimesheetEvaluation, buildTimesheetDraft, mapTimesheetEvaluation } from "./timesheetDraft";

interface ApiAllocationDay {
  work: [number | null, number | null];
  break: [number | null, number | null];
  coreHours: number;
  projectHours: Record<string, number>;
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
  projectHours: Record<string, number>;
}

const pad2 = (value: number) => value.toString().padStart(2, "0");
const minutesToTime = (value: number | null | undefined) => {
  if (value == null || value < 0) return "";
  const hours = Math.floor(value / 60);
  const minutes = value % 60;
  return `${pad2(hours)}:${pad2(minutes)}`;
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
      projectHours: day.projectHours,
    })),
    evaluation: mapTimesheetEvaluation(allocation.evaluation),
  };
};
