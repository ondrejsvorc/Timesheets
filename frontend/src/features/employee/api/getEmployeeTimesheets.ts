import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface EmployeeTimesheetItem {
  year: number;
  month: number;
  hasAttendanceImport: boolean;
  status: string | null;
}

export interface EmployeeTimesheetMonthOption {
  month: number;
}

export interface GetEmployeeTimesheetsResponse {
  employeeId: string;
  months: EmployeeTimesheetItem[];
  availableYears: number[];
  availableMonths: number[];
}

export const getEmployeeTimesheets = (employeeId: string) => {
  return {
    promise: withOptionalDelay("slow", () => customFetch<GetEmployeeTimesheetsResponse>(`${ApiUrl}/employees/${employeeId}/timesheets`)),
  };
};
