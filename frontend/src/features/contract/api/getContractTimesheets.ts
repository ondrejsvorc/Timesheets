import { redirect } from "react-router";
import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import { Texts } from "@/constants/texts";
import { getContractTimesheetsFilterOptions } from "./getContractTimesheetsFilterOptions";

export type GroupByOption = "Employee" | "Month";

export interface GetContractTimesheetsRequest {
  fromYear: number;
  fromMonth: number;
  toYear: number;
  toMonth: number;
  statuses?: string[];
}

export interface TimesheetItem {
  id: string;
  employeeId: string;
  year: number;
  month: number;
  positionCode: string | null;
  position: string;
  workload: number;
  statusId: string;
  status: string;
}

export interface EmployeeItem {
  id: string;
  personalNumber: string;
  fullName: string;
  employeeType: string;
}

export interface GetContractTimesheetsResponse {
  employees: EmployeeItem[];
  timesheets: TimesheetItem[];
}

export interface ContractTimesheetsFilterCriteria extends GetContractTimesheetsRequest {
  groupBy: GroupByOption;
}

function buildTimesheetsQuery(request: GetContractTimesheetsRequest): string {
  const params = new URLSearchParams();
  params.set("fromYear", String(request.fromYear));
  params.set("fromMonth", String(request.fromMonth));
  params.set("toYear", String(request.toYear));
  params.set("toMonth", String(request.toMonth));
  if (request.statuses?.length) {
    request.statuses.forEach((s) => {
      params.append("status", s);
    });
  }
  return params.toString();
}

export function getContractTimesheets(_projectId: string, contractId: string, request: GetContractTimesheetsRequest): Promise<GetContractTimesheetsResponse> {
  const query = buildTimesheetsQuery(request);
  const url = `${ApiUrl}/contracts/${contractId}/timesheets${query ? `?${query}` : ""}`;
  return withDelay("slow", () => customFetch<GetContractTimesheetsResponse>(url));
}

export function buildTimesheetsRequestFromUrl(url: URL): ContractTimesheetsFilterCriteria {
  const now = new Date();
  const fromYear = parseInt(url.searchParams.get("fromYear") ?? String(now.getFullYear()), 10);
  const fromMonth = parseInt(url.searchParams.get("fromMonth") ?? "1", 10);
  const toYear = parseInt(url.searchParams.get("toYear") ?? String(now.getFullYear()), 10);
  const toMonth = parseInt(url.searchParams.get("toMonth") ?? "12", 10);
  const groupBy = (url.searchParams.get("groupBy") === "Employee" ? "Employee" : "Month") as GroupByOption;
  const statusParam = url.searchParams.get("status");
  const statuses = statusParam ? statusParam.split(",").filter(Boolean) : undefined;
  return { fromYear, fromMonth, toYear, toMonth, groupBy, statuses: statuses ?? undefined };
}

export function filterToSearchParams(filter: ContractTimesheetsFilterCriteria): URLSearchParams {
  const next = new URLSearchParams();
  next.set("fromYear", String(filter.fromYear));
  next.set("fromMonth", String(filter.fromMonth));
  next.set("toYear", String(filter.toYear));
  next.set("toMonth", String(filter.toMonth));
  next.set("groupBy", filter.groupBy);
  if (filter.statuses?.length) {
    next.set("status", filter.statuses.join(","));
  }
  return next;
}

export function normalizeContractTimesheetsFilter(filter: ContractTimesheetsFilterCriteria, options: { years: number[]; months: number[] }): ContractTimesheetsFilterCriteria {
  if (options.years.length === 0 || options.months.length === 0) {
    return filter;
  }

  const years = options.years;
  const months = options.months;
  const minYear = years[0];
  const maxYear = years[years.length - 1];
  const minMonth = months[0];
  const maxMonth = months[months.length - 1];

  if (minYear === undefined || maxYear === undefined || minMonth === undefined || maxMonth === undefined) {
    return filter;
  }

  const clampYear = (y: number) => (years.includes(y) ? y : y < minYear ? minYear : maxYear);
  const clampMonth = (m: number) => (months.includes(m) ? m : m < minMonth ? minMonth : maxMonth);

  const nextFromYear = clampYear(filter.fromYear);
  const nextToYear = clampYear(filter.toYear);
  const nextFromMonth = clampMonth(filter.fromMonth);
  const nextToMonth = clampMonth(filter.toMonth);
  const invalidRange = nextFromYear > nextToYear || (nextFromYear === nextToYear && nextFromMonth > nextToMonth);

  if (invalidRange) {
    return {
      ...filter,
      fromYear: minYear,
      fromMonth: minMonth,
      toYear: maxYear,
      toMonth: maxMonth,
    };
  }

  if (nextFromYear === filter.fromYear && nextToYear === filter.toYear && nextFromMonth === filter.fromMonth && nextToMonth === filter.toMonth) {
    return filter;
  }

  return {
    ...filter,
    fromYear: nextFromYear,
    fromMonth: nextFromMonth,
    toYear: nextToYear,
    toMonth: nextToMonth,
  };
}

export async function loadContractTimesheetsPage(projectId: string, contractId: string, request: Request) {
  const url = new URL(request.url);
  const filterOptions = await getContractTimesheetsFilterOptions(contractId);
  const requestedFilter = buildTimesheetsRequestFromUrl(url);
  const filter = normalizeContractTimesheetsFilter(requestedFilter, filterOptions);

  if (filterToSearchParams(requestedFilter).toString() !== filterToSearchParams(filter).toString()) {
    throw redirect(`${url.pathname}?${filterToSearchParams(filter)}`);
  }

  return {
    filter,
    filterOptions,
    promise: getContractTimesheets(projectId, contractId, filter),
  };
}

export function statusesEqual(a: string[] | undefined, b: string[] | undefined): boolean {
  if (!a && !b) return true;
  if (!a || !b || a.length !== b.length) return false;
  const set = new Set(b);
  return a.every((s) => set.has(s));
}

export function monthInRange(year: number, month: number, fromYear: number, fromMonth: number, toYear: number, toMonth: number): boolean {
  if (year < fromYear || year > toYear) return false;
  if (year === fromYear && month < fromMonth) return false;
  if (year === toYear && month > toMonth) return false;
  return true;
}

export function rangeIsSubset(
  reqFromYear: number,
  reqFromMonth: number,
  reqToYear: number,
  reqToMonth: number,
  cacheFromYear: number,
  cacheFromMonth: number,
  cacheToYear: number,
  cacheToMonth: number,
): boolean {
  return (
    monthInRange(reqFromYear, reqFromMonth, cacheFromYear, cacheFromMonth, cacheToYear, cacheToMonth) && monthInRange(reqToYear, reqToMonth, cacheFromYear, cacheFromMonth, cacheToYear, cacheToMonth)
  );
}

export interface TimesheetRowView {
  id: string;
  position: string;
  workload: number;
  status: string;
  year?: number;
  month?: number;
}

export interface EmployeeGroupView {
  id: string;
  allTimesheetsApproved: boolean;
  personalNumber: string;
  fullName: string;
  employeeType: string;
  timesheets: TimesheetRowView[];
}

export interface MonthGroupView {
  year: number;
  month: number;
  items: EmployeeGroupView[];
}

const APPROVED_STATUS = Texts.statusApproved;

export function buildMonthsView(data: GetContractTimesheetsResponse): MonthGroupView[] {
  const byEmployee = new Map<string, EmployeeItem>();
  data.employees.forEach((e) => {
    byEmployee.set(e.id, e);
  });
  const byMonth = new Map<string, TimesheetItem[]>();
  for (const t of data.timesheets) {
    const key = `${t.year}-${t.month}`;
    const list = byMonth.get(key) ?? [];
    list.push(t);
    byMonth.set(key, list);
  }
  const months: MonthGroupView[] = [];
  for (const [key, monthTimesheets] of byMonth) {
    const [y, m] = key.split("-").map(Number) as [number, number];
    const byEmp = new Map<string, TimesheetItem[]>();
    for (const t of monthTimesheets) {
      const list = byEmp.get(t.employeeId) ?? [];
      list.push(t);
      byEmp.set(t.employeeId, list);
    }
    const items: EmployeeGroupView[] = [];
    for (const [empId, itemsTs] of byEmp) {
      const emp = byEmployee.get(empId);
      if (!emp) continue;
      const rows: TimesheetRowView[] = itemsTs.map((x) => ({
        id: x.id,
        position: x.position,
        workload: x.workload,
        status: x.status,
      }));
      items.push({
        id: emp.id,
        allTimesheetsApproved: rows.every((r) => r.status === APPROVED_STATUS),
        personalNumber: emp.personalNumber,
        fullName: emp.fullName,
        employeeType: emp.employeeType,
        timesheets: rows,
      });
    }
    items.sort((a, b) => a.fullName.localeCompare(b.fullName));
    months.push({ year: y, month: m, items });
  }
  months.sort((a, b) => (a.year !== b.year ? a.year - b.year : a.month - b.month));
  return months;
}

export function buildEmployeesView(data: GetContractTimesheetsResponse): EmployeeGroupView[] {
  const byEmployee = new Map<string, EmployeeItem>();
  data.employees.forEach((e) => {
    byEmployee.set(e.id, e);
  });
  const byEmp = new Map<string, TimesheetItem[]>();
  for (const t of data.timesheets) {
    const list = byEmp.get(t.employeeId) ?? [];
    list.push(t);
    byEmp.set(t.employeeId, list);
  }
  const out: EmployeeGroupView[] = [];
  for (const emp of data.employees) {
    const ts = byEmp.get(emp.id) ?? [];
    const rows: TimesheetRowView[] = ts.map((x) => ({
      id: x.id,
      position: x.position,
      workload: x.workload,
      status: x.status,
      year: x.year,
      month: x.month,
    }));
    out.push({
      id: emp.id,
      allTimesheetsApproved: rows.every((r) => r.status === APPROVED_STATUS),
      personalNumber: emp.personalNumber,
      fullName: emp.fullName,
      employeeType: emp.employeeType,
      timesheets: rows,
    });
  }
  out.sort((a, b) => a.fullName.localeCompare(b.fullName));
  return out;
}

export function getDeltaMonths(
  reqFromYear: number,
  reqFromMonth: number,
  reqToYear: number,
  reqToMonth: number,
  cacheFromYear: number,
  cacheFromMonth: number,
  cacheToYear: number,
  cacheToMonth: number,
): { year: number; month: number }[] {
  const out: { year: number; month: number }[] = [];
  for (let y = reqFromYear; y <= reqToYear; y++) {
    const startM = y === reqFromYear ? reqFromMonth : 1;
    const endM = y === reqToYear ? reqToMonth : 12;
    for (let m = startM; m <= endM; m++) {
      if (!monthInRange(y, m, cacheFromYear, cacheFromMonth, cacheToYear, cacheToMonth)) {
        out.push({ year: y, month: m });
      }
    }
  }
  return out;
}
