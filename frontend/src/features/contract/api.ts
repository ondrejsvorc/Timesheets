import { redirect } from "react-router";
import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import { Texts } from "@/constants/texts";
import { formatDate } from "@/utils/format";

export interface AddContractEmployeeRequest {
  employeeId: string;
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate?: string | null;
}

export interface AddContractEmployeeResponse {
  contractId: string;
  employeeId: string;
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate: string | null;
  personalNumber: string;
  fullName: string;
  employeeTypeId: string | null;
}

export interface AddContractEmployeeImpactRequest {
  employeeId: string;
  startDate: string;
  endDate?: string | null;
}

export interface AddContractEmployeeImpactResponse {
  canAdd: boolean;
  blockReason: string | null;
  submittedTimesheetCount: number;
  approvedTimesheetCount: number;
}

export interface UpdateContractEmployeeRequest {
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate: string | null;
}

export interface UpdateContractEmployeeResponse {
  id: string;
  contractId: string;
  employeeId: string;
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate: string | null;
}

export interface ContractEmployeeUpdateImpactResponse {
  canUpdate: boolean;
  createsNewAssignment: boolean;
  blockReason: string | null;
  currentAssignmentEndDate: string | null;
  newAssignmentStartDate: string | null;
  newTimesheetMonthCount: number;
  draftTimesheetsOnOldAssignment: number;
  draftDaysToRemove: number;
  submittedTimesheetCount: number;
  approvedTimesheetCount: number;
}

export interface PositionItem {
  id: string;
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate: string | null;
  isActive: boolean;
}

export interface EmployeeItem {
  id: string;
  personalNumber: string;
  fullName: string;
  employeeType: string;
  positions: PositionItem[];
}

export interface GetContractEmployeesResponse {
  projectStartDate: string;
  projectEndDate: string | null;
  isProjectArchived: boolean;
  employees: EmployeeItem[];
}

export interface GetProjectContractResponse {
  id: string;
  name: string;
  registrationNumber: string;
  projectStartDate: string;
  projectEndDate: string | null;
}

export interface GetContractTimesheetsFilterOptionsResponse {
  years: number[];
  months: number[];
  statuses: string[];
}

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

export interface ContractTimesheetEmployeeItem {
  id: string;
  personalNumber: string;
  fullName: string;
  employeeType: string;
}

export interface GetContractTimesheetsResponse {
  employees: ContractTimesheetEmployeeItem[];
  timesheets: TimesheetItem[];
}

export interface ContractTimesheetsFilterCriteria extends GetContractTimesheetsRequest {
  groupBy: GroupByOption;
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

const ApprovedStatus = Texts.statusApproved;
const formatCount = (template: string, count: number) => template.replace("{count}", String(count));

export const addContractEmployee = (contractId: string, request: AddContractEmployeeRequest, signal?: AbortSignal): Promise<AddContractEmployeeResponse> => {
  return customFetch<AddContractEmployeeResponse>(`${ApiUrl}/contracts/${contractId}/employees`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
    signal,
  });
};

export const getAddContractEmployeeImpact = (contractId: string, request: AddContractEmployeeImpactRequest, signal?: AbortSignal): Promise<AddContractEmployeeImpactResponse> => {
  return withDelay("fast", () => {
    return customFetch<AddContractEmployeeImpactResponse>(`${ApiUrl}/contracts/${contractId}/employees/add-impact`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
      signal,
    });
  });
};

export const formatAddImpactConsequences = (impact: AddContractEmployeeImpactResponse): string[] => {
  if (!impact.canAdd) {
    return impact.blockReason ? [impact.blockReason] : [Texts.addImpactBlocked];
  }

  const consequences: string[] = [];
  if (impact.submittedTimesheetCount > 0) {
    consequences.push(formatCount(Texts.updateImpactSubmitted, impact.submittedTimesheetCount));
  }
  if (impact.approvedTimesheetCount > 0) {
    consequences.push(formatCount(Texts.updateImpactApproved, impact.approvedTimesheetCount));
  }

  return consequences.length > 0 ? consequences : [Texts.addImpactOk];
};

export const updateContractEmployee = (contractId: string, contractEmployeeId: string, request: UpdateContractEmployeeRequest, signal?: AbortSignal): Promise<UpdateContractEmployeeResponse> => {
  return customFetch<UpdateContractEmployeeResponse>(`${ApiUrl}/contracts/${contractId}/employees/${contractEmployeeId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
    signal,
  });
};

export const getContractEmployeeUpdateImpact = (
  contractId: string,
  contractEmployeeId: string,
  request: UpdateContractEmployeeRequest,
  signal?: AbortSignal,
): Promise<ContractEmployeeUpdateImpactResponse> => {
  return withDelay("fast", () => {
    return customFetch<ContractEmployeeUpdateImpactResponse>(`${ApiUrl}/contracts/${contractId}/employees/${contractEmployeeId}/update-impact`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
      signal,
    });
  });
};

export const formatUpdateImpactConsequences = (impact: ContractEmployeeUpdateImpactResponse): string[] => {
  if (!impact.canUpdate) {
    return impact.blockReason ? [impact.blockReason] : [Texts.updateImpactBlocked];
  }

  const consequences: string[] = [];

  if (impact.createsNewAssignment) {
    if (impact.currentAssignmentEndDate) {
      consequences.push(Texts.updateImpactEndOld.replace("{date}", formatDate(impact.currentAssignmentEndDate)));
    }

    if (impact.newAssignmentStartDate) {
      consequences.push(Texts.updateImpactStartNew.replace("{date}", formatDate(impact.newAssignmentStartDate)));
    }
  } else if (impact.currentAssignmentEndDate) {
    consequences.push(Texts.updateImpactNewEnd.replace("{date}", formatDate(impact.currentAssignmentEndDate)));
  }

  if (impact.newTimesheetMonthCount > 0) {
    consequences.push(formatCount(Texts.updateImpactNewMonths, impact.newTimesheetMonthCount));
  }

  if (impact.draftDaysToRemove > 0) {
    consequences.push(formatCount(Texts.updateImpactDraftDaysRemove, impact.draftDaysToRemove));
  }

  if (impact.draftTimesheetsOnOldAssignment > 0) {
    consequences.push(formatCount(Texts.updateImpactDraftsOnOld, impact.draftTimesheetsOnOldAssignment));
  }

  if (impact.submittedTimesheetCount > 0) {
    consequences.push(formatCount(Texts.updateImpactSubmitted, impact.submittedTimesheetCount));
  }

  if (impact.approvedTimesheetCount > 0) {
    consequences.push(formatCount(Texts.updateImpactApproved, impact.approvedTimesheetCount));
  }

  return consequences;
};

export const deleteContractEmployee = (contractId: string, contractEmployeeId: string, signal?: AbortSignal): Promise<void> => {
  return customFetch<void>(`${ApiUrl}/contracts/${contractId}/employees/${contractEmployeeId}`, {
    method: "DELETE",
    signal,
  });
};

export const getContractEmployees = (_projectId: string, contractId: string): Promise<GetContractEmployeesResponse> => {
  return withDelay("slow", () => {
    return customFetch<GetContractEmployeesResponse>(`${ApiUrl}/contracts/${contractId}/employees`);
  });
};

export const getProjectContract = (projectId: string, contractId: string): Promise<GetProjectContractResponse | null> => {
  return withDelay("fast", async () => {
    try {
      return await customFetch<GetProjectContractResponse>(`${ApiUrl}/projects/${projectId}/contracts/${contractId}`);
    } catch (error) {
      if (error instanceof Error && error.message.startsWith("404")) {
        return null;
      }
      throw error;
    }
  });
};

export function getContractTimesheetsFilterOptions(contractId: string): Promise<GetContractTimesheetsFilterOptionsResponse> {
  const url = `${ApiUrl}/contracts/${contractId}/timesheets/filter-options`;
  return withDelay("slow", () => {
    return customFetch<GetContractTimesheetsFilterOptionsResponse>(url);
  });
}

function buildTimesheetsQuery(request: GetContractTimesheetsRequest): string {
  const params = new URLSearchParams();
  params.set("fromYear", String(request.fromYear));
  params.set("fromMonth", String(request.fromMonth));
  params.set("toYear", String(request.toYear));
  params.set("toMonth", String(request.toMonth));
  if (request.statuses?.length) {
    request.statuses.forEach((status) => {
      params.append("status", status);
    });
  }
  return params.toString();
}

export function getContractTimesheets(_projectId: string, contractId: string, request: GetContractTimesheetsRequest): Promise<GetContractTimesheetsResponse> {
  const query = buildTimesheetsQuery(request);
  const url = `${ApiUrl}/contracts/${contractId}/timesheets${query ? `?${query}` : ""}`;
  return withDelay("slow", () => {
    return customFetch<GetContractTimesheetsResponse>(url);
  });
}

export function buildTimesheetsRequestFromUrl(url: URL): ContractTimesheetsFilterCriteria {
  const now = new Date();
  const fromYear = Number.parseInt(url.searchParams.get("fromYear") ?? String(now.getFullYear()), 10);
  const fromMonth = Number.parseInt(url.searchParams.get("fromMonth") ?? "1", 10);
  const toYear = Number.parseInt(url.searchParams.get("toYear") ?? String(now.getFullYear()), 10);
  const toMonth = Number.parseInt(url.searchParams.get("toMonth") ?? "12", 10);
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

  const clampYear = (year: number) => (years.includes(year) ? year : year < minYear ? minYear : maxYear);
  const clampMonth = (month: number) => (months.includes(month) ? month : month < minMonth ? minMonth : maxMonth);

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

export function statusesEqual(left: string[] | undefined, right: string[] | undefined): boolean {
  if (!left && !right) return true;
  if (!left || !right || left.length !== right.length) return false;
  const set = new Set(right);
  return left.every((status) => set.has(status));
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

export function buildMonthsView(data: GetContractTimesheetsResponse): MonthGroupView[] {
  const byEmployee = new Map<string, ContractTimesheetEmployeeItem>();
  data.employees.forEach((employee) => {
    byEmployee.set(employee.id, employee);
  });
  const byMonth = new Map<string, TimesheetItem[]>();
  for (const timesheet of data.timesheets) {
    const key = `${timesheet.year}-${timesheet.month}`;
    const list = byMonth.get(key) ?? [];
    list.push(timesheet);
    byMonth.set(key, list);
  }
  const months: MonthGroupView[] = [];
  for (const [key, monthTimesheets] of byMonth) {
    const [year, month] = key.split("-").map(Number) as [number, number];
    const byEmployeeId = new Map<string, TimesheetItem[]>();
    for (const timesheet of monthTimesheets) {
      const list = byEmployeeId.get(timesheet.employeeId) ?? [];
      list.push(timesheet);
      byEmployeeId.set(timesheet.employeeId, list);
    }
    const items: EmployeeGroupView[] = [];
    for (const [employeeId, employeeTimesheets] of byEmployeeId) {
      const employee = byEmployee.get(employeeId);
      if (!employee) continue;
      const rows: TimesheetRowView[] = employeeTimesheets.map((timesheet) => ({
        id: timesheet.id,
        position: timesheet.position,
        workload: timesheet.workload,
        status: timesheet.status,
      }));
      items.push({
        id: employee.id,
        allTimesheetsApproved: rows.every((row) => row.status === ApprovedStatus),
        personalNumber: employee.personalNumber,
        fullName: employee.fullName,
        employeeType: employee.employeeType,
        timesheets: rows,
      });
    }
    items.sort((left, right) => left.fullName.localeCompare(right.fullName));
    months.push({ year, month, items });
  }
  months.sort((left, right) => (left.year !== right.year ? left.year - right.year : left.month - right.month));
  return months;
}

export function buildEmployeesView(data: GetContractTimesheetsResponse): EmployeeGroupView[] {
  const byEmployee = new Map<string, ContractTimesheetEmployeeItem>();
  data.employees.forEach((employee) => {
    byEmployee.set(employee.id, employee);
  });
  const byEmployeeId = new Map<string, TimesheetItem[]>();
  for (const timesheet of data.timesheets) {
    const list = byEmployeeId.get(timesheet.employeeId) ?? [];
    list.push(timesheet);
    byEmployeeId.set(timesheet.employeeId, list);
  }
  const output: EmployeeGroupView[] = [];
  for (const employee of data.employees) {
    const timesheets = byEmployeeId.get(employee.id) ?? [];
    const rows: TimesheetRowView[] = timesheets.map((timesheet) => ({
      id: timesheet.id,
      position: timesheet.position,
      workload: timesheet.workload,
      status: timesheet.status,
      year: timesheet.year,
      month: timesheet.month,
    }));
    output.push({
      id: employee.id,
      allTimesheetsApproved: rows.every((row) => row.status === ApprovedStatus),
      personalNumber: employee.personalNumber,
      fullName: employee.fullName,
      employeeType: employee.employeeType,
      timesheets: rows,
    });
  }
  output.sort((left, right) => left.fullName.localeCompare(right.fullName));
  return output;
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
  const output: { year: number; month: number }[] = [];
  for (let year = reqFromYear; year <= reqToYear; year++) {
    const startMonth = year === reqFromYear ? reqFromMonth : 1;
    const endMonth = year === reqToYear ? reqToMonth : 12;
    for (let month = startMonth; month <= endMonth; month++) {
      if (!monthInRange(year, month, cacheFromYear, cacheFromMonth, cacheToYear, cacheToMonth)) {
        output.push({ year, month });
      }
    }
  }
  return output;
}
