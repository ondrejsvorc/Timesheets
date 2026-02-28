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

const mockResponse: GetContractTimesheetsResponse = {
  employees: [
    {
      id: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c",
      allTimesheetsApproved: true,
      personalNumber: 10001,
      fullName: "Jan Novák",
      employeeType: "Akademik",
      timesheets: [
        {
          id: "t1a1b1c1-d1e1-4f1a-8b1c-000000000001",
          position: "01-01 – Senior Software Engineer",
          workload: 1.0,
          statusId: "s-approved",
          status: "Schválený",
        },
        {
          id: "t1a1b1c1-d1e1-4f1a-8b1c-000000000002",
          position: "01-01 – Senior Software Engineer",
          workload: 1.0,
          statusId: "s-approved",
          status: "Schválený",
        },
      ],
    },
    {
      id: "e2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d",
      allTimesheetsApproved: false,
      personalNumber: 10002,
      fullName: "Marie Svobodová",
      employeeType: "Neakademik",
      timesheets: [
        {
          id: "t2a2b2c2-d2e2-4f2a-8b2c-000000000003",
          position: "01-02 – Analyst",
          workload: 0.5,
          statusId: "s-draft",
          status: "Koncept",
        },
      ],
    },
  ],
  months: [
    {
      year: 2024,
      month: 10,
      items: [
        {
          id: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c",
          allTimesheetsApproved: true,
          personalNumber: 10001,
          fullName: "Jan Novák",
          employeeType: "Akademik",
          timesheets: [
            {
              id: "t1a1b1c1-d1e1-4f1a-8b1c-000000000001",
              position: "01-01 – Senior Software Engineer",
              workload: 1.0,
              statusId: "s-approved",
              status: "Schválený",
            },
          ],
        },
      ],
    },
  ],
};

export const getContractTimesheets = (
  projectId: string,
  contractId: string,
  request?: GetContractTimesheetsRequest,
) => {
  return {
    promise: (async (): Promise<GetContractTimesheetsResponse> => {
      await new Promise((resolve) => setTimeout(resolve, 1200));
      return mockResponse;
    })(),
  };
};
