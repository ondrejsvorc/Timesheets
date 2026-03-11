import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface EmployeeTimesheetItem {
  id: string;
  contractId: string;
  contractName: string;
  year: number;
  month: number;
  statusId: string;
  status: string;
}

export interface GetEmployeeTimesheetsResponse {
  employeeId: string;
  timesheets: EmployeeTimesheetItem[];
}

export const getEmployeeTimesheets = (employeeId: string, year?: number, months?: number[]) => {
  const params = new URLSearchParams();

  if (year !== undefined) {
    params.set("year", String(year));
  }

  if (months && months.length > 0) {
    params.set("months", months.join(","));
  }

  const query = params.toString();
  const url = query ? `${ApiUrl}/employees/${employeeId}/timesheets?${query}` : `${ApiUrl}/employees/${employeeId}/timesheets`;

  return {
    promise: withOptionalDelay("slow", () => customFetch<GetEmployeeTimesheetsResponse>(url)),
  };
};
