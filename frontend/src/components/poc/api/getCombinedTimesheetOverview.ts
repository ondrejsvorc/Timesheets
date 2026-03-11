import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface CombinedTimesheetOverviewItem {
  label: string;
  contractName: string | null;
  position: string | null;
  workload: number;
  managers: string[];
}

export interface GetCombinedTimesheetOverviewResponse {
  employeeId: string;
  year: number;
  month: number;
  items: CombinedTimesheetOverviewItem[];
}

export const getCombinedTimesheetOverview = (employeeId: string, year: number, month: number) => {
  const params = new URLSearchParams({
    employeeId,
    year: String(year),
    month: String(month),
  });

  return {
    promise: withOptionalDelay("slow", () => customFetch<GetCombinedTimesheetOverviewResponse>(`${ApiUrl}/timesheets/combined/overview?${params.toString()}`)),
  };
};
