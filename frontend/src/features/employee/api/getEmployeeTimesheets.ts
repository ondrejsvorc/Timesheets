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

export interface EmployeeTimesheetMonthOption {
  year: number;
  month: number;
  hasUnapproved: boolean;
}

export interface GetEmployeeTimesheetsResponse {
  employeeId: string;
  timesheets: EmployeeTimesheetItem[];
  availableYears: number[];
  availableMonths: EmployeeTimesheetMonthOption[];
}

export const getEmployeeTimesheets = (employeeId: string) => {
  return {
    promise: withOptionalDelay("slow", () => customFetch<GetEmployeeTimesheetsResponse>(`${ApiUrl}/employees/${employeeId}/timesheets`)),
  };
};
