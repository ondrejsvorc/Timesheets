export type GroupByOption = "Employee" | "Month";

/** Request sent to API (no groupBy – grouping is frontend-only). */
export interface GetContractTimesheetsRequest {
  fromYear: number;
  fromMonth: number;
  toYear: number;
  toMonth: number;
  statuses?: string[];
}

/** One row per timesheet. */
export interface TimesheetItem {
  id: string;
  employeeId: string;
  year: number;
  month: number;
  position: string | null;
  workload: number | null;
  statusId: string;
  status: string;
}

/** Unique employees in the result (join by id). */
export interface EmployeeItem {
  id: string;
  personalNumber: number;
  fullName: string;
  employeeType: string;
}

export interface GetContractTimesheetsResponse {
  employees: EmployeeItem[];
  timesheets: TimesheetItem[];
}

/** Filter state in UI (includes groupBy for view only). */
export interface ContractTimesheetsFilterCriteria extends GetContractTimesheetsRequest {
  groupBy: GroupByOption;
}

const MOCK_EMPLOYEES: EmployeeItem[] = [
  { id: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c", personalNumber: 2154, fullName: "Jan Novák", employeeType: "Neakademik" },
  { id: "e2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d", personalNumber: 2987, fullName: "Petra Malá", employeeType: "Akademik" },
  { id: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e", personalNumber: 2647, fullName: "Karel Nový", employeeType: "Neakademik" },
];

const STATUS_IN_PROGRESS = "Rozpracovaný";
const STATUS_PENDING = "Ke schválení";
const STATUS_APPROVED = "Schválený";

const MOCK_TIMESHEETS: TimesheetItem[] = [
  { id: "t1", employeeId: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c", year: 2025, month: 1, position: "01-01 název pozice", workload: 0.1, statusId: "s1", status: STATUS_IN_PROGRESS },
  { id: "t2", employeeId: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c", year: 2025, month: 1, position: "01-02 název pozice", workload: 0.2, statusId: "s1", status: STATUS_IN_PROGRESS },
  { id: "t3", employeeId: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c", year: 2025, month: 2, position: "01-01 název pozice", workload: 0.7, statusId: "s2", status: STATUS_PENDING },
  { id: "t4", employeeId: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c", year: 2025, month: 3, position: "01-02 název pozice", workload: 0.4, statusId: "s3", status: STATUS_APPROVED },
  { id: "t5", employeeId: "e2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d", year: 2025, month: 2, position: "01-01 název pozice", workload: 0.7, statusId: "s2", status: STATUS_PENDING },
  { id: "t6", employeeId: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e", year: 2025, month: 1, position: "01-01 název pozice", workload: 0.1, statusId: "s3", status: STATUS_APPROVED },
  { id: "t7", employeeId: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e", year: 2025, month: 1, position: "01-02 název pozice", workload: 0.2, statusId: "s3", status: STATUS_APPROVED },
  { id: "t8", employeeId: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e", year: 2025, month: 2, position: "01-01 název pozice", workload: 0.7, statusId: "s3", status: STATUS_APPROVED },
  { id: "t9", employeeId: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e", year: 2025, month: 3, position: "01-02 název pozice", workload: 0.4, statusId: "s3", status: STATUS_APPROVED },
];

function filterTimesheetsByRequest(
  timesheets: TimesheetItem[],
  request: GetContractTimesheetsRequest,
): TimesheetItem[] {
  return timesheets.filter((t) => {
    if (t.year < request.fromYear || (t.year === request.fromYear && t.month < request.fromMonth)) return false;
    if (t.year > request.toYear || (t.year === request.toYear && t.month > request.toMonth)) return false;
    if (request.statuses?.length && !request.statuses.includes(t.status)) return false;
    return true;
  });
}

/** Mock: returns flat employees + timesheets for the requested range (and status filter). */
export function getContractTimesheetsMock(
  _projectId: string,
  _contractId: string,
  request: GetContractTimesheetsRequest,
): Promise<GetContractTimesheetsResponse> {
  return new Promise((resolve) => {
    setTimeout(() => {
      const filtered = filterTimesheetsByRequest(MOCK_TIMESHEETS, request);
      const employeeIds = [...new Set(filtered.map((t) => t.employeeId))];
      const employees = MOCK_EMPLOYEES.filter((e) => employeeIds.includes(e.id));
      resolve({ employees, timesheets: filtered });
    }, 800);
  });
}

/** Lokálně vždy mock. Reálné API volání se zatím neřeší. */
export function getContractTimesheets(
  projectId: string,
  contractId: string,
  request: GetContractTimesheetsRequest,
): Promise<GetContractTimesheetsResponse> {
  return getContractTimesheetsMock(projectId, contractId, request);
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

/** Compare statuses array (order-independent). */
export function statusesEqual(a: string[] | undefined, b: string[] | undefined): boolean {
  if (!a && !b) return true;
  if (!a || !b || a.length !== b.length) return false;
  const set = new Set(b);
  return a.every((s) => set.has(s));
}

/** True if (year, month) is inside [fromYear/fromMonth, toYear/toMonth] inclusive. */
export function monthInRange(
  year: number,
  month: number,
  fromYear: number,
  fromMonth: number,
  toYear: number,
  toMonth: number,
): boolean {
  if (year < fromYear || year > toYear) return false;
  if (year === fromYear && month < fromMonth) return false;
  if (year === toYear && month > toMonth) return false;
  return true;
}

/** Request range is fully inside cached range. */
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
    monthInRange(reqFromYear, reqFromMonth, cacheFromYear, cacheFromMonth, cacheToYear, cacheToMonth) &&
    monthInRange(reqToYear, reqToMonth, cacheFromYear, cacheFromMonth, cacheToYear, cacheToMonth)
  );
}

/** View shape for UI: one row in a table (no employeeId/year/month in month view). */
export interface TimesheetRowView {
  id: string;
  position: string | null;
  workload: number | null;
  status: string;
  year?: number;
  month?: number;
}

/** View shape for UI: employee with timesheets (for both month and employee views). */
export interface EmployeeGroupView {
  id: string;
  allTimesheetsApproved: boolean;
  personalNumber: number;
  fullName: string;
  employeeType: string;
  timesheets: TimesheetRowView[];
}

export interface MonthGroupView {
  year: number;
  month: number;
  items: EmployeeGroupView[];
}

const APPROVED_STATUS = "Schválený";

/** Build "by month" view from flat data. */
export function buildMonthsView(data: GetContractTimesheetsResponse): MonthGroupView[] {
  const byEmployee = new Map<string, EmployeeItem>();
  data.employees.forEach((e) => byEmployee.set(e.id, e));
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

/** Build "by employee" view from flat data. */
export function buildEmployeesView(data: GetContractTimesheetsResponse): EmployeeGroupView[] {
  const byEmployee = new Map<string, EmployeeItem>();
  data.employees.forEach((e) => byEmployee.set(e.id, e));
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

/** Months in [req] that are outside [cache]. Returns list of { year, month } to fetch. */
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
