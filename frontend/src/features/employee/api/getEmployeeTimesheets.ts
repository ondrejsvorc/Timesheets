import { redirect } from "react-router";
import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import { normalizeEmployeeTimesheetsFilter } from "../utils/normalizeEmployeeTimesheetsFilter";

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

export const getEmployeeTimesheets = (employeeId: string) => {
  return {
    promise: withOptionalDelay("slow", () => customFetch<GetEmployeeTimesheetsResponse>(`${ApiUrl}/employees/${employeeId}/timesheets`)),
  };
};

export async function loadEmployeeTimesheetsPage(employeeId: string, request: Request) {
  const url = new URL(request.url);
  const requestedFilter = buildEmployeeTimesheetsFilterFromUrl(url);
  const response = await getEmployeeTimesheets(employeeId).promise;
  const filter = normalizeEmployeeTimesheetsFilter(requestedFilter, response);

  if (filterToSearchParams(requestedFilter).toString() !== filterToSearchParams(filter).toString()) {
    throw redirect(`${url.pathname}?${filterToSearchParams(filter)}`);
  }

  return {
    filter,
    promise: Promise.resolve(response),
  };
}
