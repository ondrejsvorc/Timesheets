import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export interface CombinedTimesheetOverviewItem {
  timesheetId: string | null;
  kind: "core" | "project";
  label: string;
  contractRegistrationNumber: string | null;
  position: string | null;
  workload: number;
  managers: string[];
  status: string;
  contractId: string | null;
  projectId: string | null;
}

export interface CombinedTimesheetMonthSummary {
  periodStart: string;
  periodEnd: string;
  workdays: number;
  vacationDays: number;
  sickDays: number;
  holidays: number;
  totalWorkload: number;
}

export interface GetCombinedTimesheetOverviewResponse {
  employeeId: string;
  year: number;
  month: number;
  status: string;
  items: CombinedTimesheetOverviewItem[];
  summary: CombinedTimesheetMonthSummary;
}

export const getCombinedTimesheetOverview = (employeeId: string, year: number, month: number) => {
  const params = new URLSearchParams({
    employeeId,
    year: String(year),
    month: String(month),
  });

  return withDelay("slow", () => customFetch<GetCombinedTimesheetOverviewResponse>(`${ApiUrl}/timesheets/combined/overview?${params.toString()}`));
};
