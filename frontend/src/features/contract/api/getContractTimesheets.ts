export type GroupByOption = "Employee" | "Month";

export interface GetContractTimesheetsRequest {
  fromYear: number;
  fromMonth: number;
  toYear: number;
  toMonth: number;
  groupBy: GroupByOption;
  statuses?: string[] | null;
}

export interface TimesheetItem {
  id: string;
  position: string | null;
  workload: number | null;
  statusId: string;
  status: string;
  /** Pro zobrazení sloupce Období při seskupení dle zaměstnance (backend může doplnit později). */
  year?: number;
  month?: number;
}

export interface EmployeeGroup {
  id: string;
  allTimesheetsApproved: boolean;
  personalNumber: number | null;
  fullName: string;
  employeeType: string;
  timesheets: TimesheetItem[];
}

export interface MonthGroup {
  year: number;
  month: number;
  items: EmployeeGroup[];
}

export interface GetContractTimesheetsResponse {
  employees: EmployeeGroup[];
  months: MonthGroup[];
}

const STATUS_IN_PROGRESS = "Rozpracovaný";
const STATUS_PENDING = "Ke schválení";
const STATUS_APPROVED = "Schválený";

const mockEmployees: EmployeeGroup[] = [
  {
    id: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c",
    allTimesheetsApproved: false,
    personalNumber: 2154,
    fullName: "Jan Novák",
    employeeType: "Neakademik",
    timesheets: [
      { id: "t1", position: "01-01 název pozice", workload: 0.1, statusId: "s1", status: STATUS_IN_PROGRESS, year: 2025, month: 1 },
      { id: "t2", position: "01-02 název pozice", workload: 0.2, statusId: "s1", status: STATUS_IN_PROGRESS, year: 2025, month: 1 },
      { id: "t3", position: "01-01 název pozice", workload: 0.7, statusId: "s2", status: STATUS_PENDING, year: 2025, month: 2 },
      { id: "t4", position: "01-02 název pozice", workload: 0.4, statusId: "s3", status: STATUS_APPROVED, year: 2025, month: 3 },
    ],
  },
  {
    id: "e2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d",
    allTimesheetsApproved: false,
    personalNumber: 2987,
    fullName: "Petra Malá",
    employeeType: "Akademik",
    timesheets: [
      { id: "t5", position: "01-01 název pozice", workload: 0.7, statusId: "s2", status: STATUS_PENDING, year: 2025, month: 2 },
    ],
  },
  {
    id: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e",
    allTimesheetsApproved: true,
    personalNumber: 2647,
    fullName: "Karel Nový",
    employeeType: "Neakademik",
    timesheets: [
      { id: "t6", position: "01-01 název pozice", workload: 0.1, statusId: "s3", status: STATUS_APPROVED, year: 2025, month: 1 },
      { id: "t7", position: "01-02 název pozice", workload: 0.2, statusId: "s3", status: STATUS_APPROVED, year: 2025, month: 1 },
      { id: "t8", position: "01-01 název pozice", workload: 0.7, statusId: "s3", status: STATUS_APPROVED, year: 2025, month: 2 },
      { id: "t9", position: "01-02 název pozice", workload: 0.4, statusId: "s3", status: STATUS_APPROVED, year: 2025, month: 3 },
    ],
  },
];

function buildMockMonths(): MonthGroup[] {
  return [
    {
      year: 2025,
      month: 1,
      items: [
        {
          id: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c",
          allTimesheetsApproved: false,
          personalNumber: 2154,
          fullName: "Jan Novák",
          employeeType: "Neakademik",
          timesheets: [
            { id: "t1", position: "01-01 název pozice", workload: 0.1, statusId: "s1", status: STATUS_IN_PROGRESS },
            { id: "t2", position: "01-02 název pozice", workload: 0.2, statusId: "s1", status: STATUS_IN_PROGRESS },
          ],
        },
        {
          id: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e",
          allTimesheetsApproved: true,
          personalNumber: 2647,
          fullName: "Karel Nový",
          employeeType: "Neakademik",
          timesheets: [
            { id: "t6", position: "01-01 název pozice", workload: 0.1, statusId: "s3", status: STATUS_APPROVED },
            { id: "t7", position: "01-02 název pozice", workload: 0.2, statusId: "s3", status: STATUS_APPROVED },
          ],
        },
      ],
    },
    {
      year: 2025,
      month: 2,
      items: [
        {
          id: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c",
          allTimesheetsApproved: false,
          personalNumber: 2154,
          fullName: "Jan Novák",
          employeeType: "Neakademik",
          timesheets: [{ id: "t3", position: "01-01 název pozice", workload: 0.7, statusId: "s2", status: STATUS_PENDING }],
        },
        {
          id: "e2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d",
          allTimesheetsApproved: false,
          personalNumber: 2987,
          fullName: "Petra Malá",
          employeeType: "Akademik",
          timesheets: [{ id: "t5", position: "01-01 název pozice", workload: 0.7, statusId: "s2", status: STATUS_PENDING }],
        },
        {
          id: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e",
          allTimesheetsApproved: true,
          personalNumber: 2647,
          fullName: "Karel Nový",
          employeeType: "Neakademik",
          timesheets: [{ id: "t8", position: "01-01 název pozice", workload: 0.7, statusId: "s3", status: STATUS_APPROVED }],
        },
      ],
    },
    {
      year: 2025,
      month: 3,
      items: [
        {
          id: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c",
          allTimesheetsApproved: true,
          personalNumber: 2154,
          fullName: "Jan Novák",
          employeeType: "Neakademik",
          timesheets: [{ id: "t4", position: "01-02 název pozice", workload: 0.4, statusId: "s3", status: STATUS_APPROVED }],
        },
        {
          id: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e",
          allTimesheetsApproved: true,
          personalNumber: 2647,
          fullName: "Karel Nový",
          employeeType: "Neakademik",
          timesheets: [{ id: "t9", position: "01-02 název pozice", workload: 0.4, statusId: "s3", status: STATUS_APPROVED }],
        },
      ],
    },
  ];
}

export function buildTimesheetsRequestFromUrl(url: URL): GetContractTimesheetsRequest {
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

export const getContractTimesheets = (
  _projectId: string,
  _contractId: string,
  request?: GetContractTimesheetsRequest,
) => {
  return {
    promise: (async (): Promise<GetContractTimesheetsResponse> => {
      await new Promise((resolve) => setTimeout(resolve, 1200));
      const groupBy = request?.groupBy ?? "Month";
      if (groupBy === "Employee") {
        return { employees: mockEmployees, months: [] };
      }
      return { employees: [], months: buildMockMonths() };
    })(),
  };
};
