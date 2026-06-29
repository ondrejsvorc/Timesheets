import { redirect } from "react-router";
import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import { normalizeEmployeeTimesheetsFilter } from "./utils/normalizeEmployeeTimesheetsFilter";

export interface EmployeeItem {
  id: string;
  employeeTypeId: string | null;
  fullName: string;
  personalNumber: string;
}

export interface GetEmployeeResponse {
  employee: EmployeeItem;
}

export interface EmployeePositionItem {
  id: string;
  projectId: string;
  projectName: string;
  projectStartDate: string;
  projectEndDate: string | null;
  contractId: string;
  contractRegistrationNumber: string;
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate: string | null;
}

export interface GetEmployeePositionsResponse {
  employeeId: string;
  positions: EmployeePositionItem[];
}

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

export interface EmployeeTimesheetsFilterCriteria {
  year: number;
  months: number[] | null;
}

export interface ImportResult {
  fileName: string;
  success: boolean;
  errorMessage: string | null;
  timesheetId: string | null;
  year: number | null;
  month: number | null;
}

export interface TimesheetDetectionResult {
  fileName: string;
  canImport: boolean;
  isReimport: boolean;
  errorMessage: string | null;
  employeePersonalNumber: string | null;
  employeeName: string | null;
  year: number | null;
  month: number | null;
}

interface ImportTimesheetResponse {
  result: ImportResult;
}

interface DetectTimesheetResponse {
  result: TimesheetDetectionResult;
}

export const getEmployee = (employeeId: string): Promise<GetEmployeeResponse> => {
  return withDelay("fast", async () => {
    const employee = await customFetch<EmployeeItem>(`${ApiUrl}/employees/${employeeId}`);
    return { employee };
  });
};

export const getEmployeePositions = (employeeId: string): Promise<GetEmployeePositionsResponse> => {
  return withDelay("slow", () => {
    return customFetch<GetEmployeePositionsResponse>(`${ApiUrl}/employees/${employeeId}/positions`);
  });
};

export const getEmployeeTimesheets = (employeeId: string): Promise<GetEmployeeTimesheetsResponse> => {
  return withDelay("slow", () => {
    return customFetch<GetEmployeeTimesheetsResponse>(`${ApiUrl}/employees/${employeeId}/timesheets`);
  });
};

export function buildEmployeeTimesheetsFilterFromUrl(url: URL): EmployeeTimesheetsFilterCriteria {
  const now = new Date();
  const year = Number.parseInt(url.searchParams.get("year") ?? String(now.getFullYear()), 10);
  const monthsParam = url.searchParams.get("months");
  const months =
    monthsParam === null || monthsParam === ""
      ? null
      : monthsParam
          .split(",")
          .map((month) => Number.parseInt(month, 10))
          .filter((month) => !Number.isNaN(month));
  return { year, months };
}

export function filterToSearchParams(filter: EmployeeTimesheetsFilterCriteria): URLSearchParams {
  const next = new URLSearchParams();
  next.set("year", String(filter.year));
  if (filter.months !== null && filter.months.length > 0) {
    next.set("months", filter.months.join(","));
  }
  return next;
}

export async function loadEmployeeTimesheetsPage(employeeId: string, request: Request) {
  const url = new URL(request.url);
  const requestedFilter = buildEmployeeTimesheetsFilterFromUrl(url);
  const response = await getEmployeeTimesheets(employeeId);
  const filter = normalizeEmployeeTimesheetsFilter(requestedFilter, response);

  if (filterToSearchParams(requestedFilter).toString() !== filterToSearchParams(filter).toString()) {
    throw redirect(`${url.pathname}?${filterToSearchParams(filter)}`);
  }

  return {
    filter,
    promise: Promise.resolve(response),
  };
}

export const detectTimesheetImport = async (employeeId: string, file: File, signal?: AbortSignal): Promise<TimesheetDetectionResult> => {
  const formData = new FormData();
  formData.append("employeeId", employeeId);
  formData.append("file", file);

  const response = await customFetch<DetectTimesheetResponse>(`${ApiUrl}/timesheets/detect`, { method: "POST", body: formData, signal });
  return response.result;
};

export const importTimesheet = async (employeeId: string, file: File, signal?: AbortSignal): Promise<ImportResult> => {
  const formData = new FormData();
  formData.append("employeeId", employeeId);
  formData.append("file", file);

  const response = await customFetch<ImportTimesheetResponse>(`${ApiUrl}/timesheets/`, { method: "POST", body: formData, signal });
  return response.result;
};
