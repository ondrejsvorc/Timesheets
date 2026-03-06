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

const STATUS_IN_PROGRESS = "Rozpracovaný";
const STATUS_PENDING = "Ke schválení";
const STATUS_APPROVED = "Schválený";

// Generate mock data for all 12 months of 2025
const generateMockTimesheets = (): EmployeeTimesheetItem[] => {
  const timesheets: EmployeeTimesheetItem[] = [];
  const contracts = [
    { id: "a1c3e5f7-1111-4a2b-9c3d-000000000001", name: "Projektová činnost 1" },
    { id: "a1c3e5f7-2222-4a2b-9c3d-000000000002", name: "Projektová činnost 2" },
    { id: "a1c3e5f7-3333-4a2b-9c3d-000000000003", name: "Projektová činnost 3" },
  ];

  for (let month = 1; month <= 12; month++) {
    contracts.forEach((contract, index) => {
      let status: string;
      let statusId: string;
      
      // Vary statuses: months 10-12 have unapproved, others are mostly approved
      if (month >= 10) {
        if (index === 0) {
          status = STATUS_IN_PROGRESS;
          statusId = "status-1";
        } else if (index === 1) {
          status = STATUS_PENDING;
          statusId = "status-2";
        } else {
          status = STATUS_APPROVED;
          statusId = "status-3";
        }
      } else {
        // Some earlier months also have unapproved for variety
        if (month === 1 && index === 0) {
          status = STATUS_PENDING;
          statusId = "status-2";
        } else if (month === 3 && index === 1) {
          status = STATUS_IN_PROGRESS;
          statusId = "status-1";
        } else {
          status = STATUS_APPROVED;
          statusId = "status-3";
        }
      }

      timesheets.push({
        id: `t-${month}-${index + 1}`,
        contractId: contract.id,
        contractName: contract.name,
        year: 2025,
        month,
        statusId,
        status,
      });
    });
  }

  return timesheets;
};

const mockResponse: GetEmployeeTimesheetsResponse = {
  employeeId: "1f4a3b2c-8e6d-4b2a-9f3e-1c2d3e4f5a6b",
  timesheets: generateMockTimesheets(),
};

export const getEmployeeTimesheets = (employeeId: string, year?: number, months?: number[]) => {
  return {
    promise: (async (): Promise<GetEmployeeTimesheetsResponse> => {
      // TODO: Replace with real API call
      // const params = new URLSearchParams();
      // if (year) params.set('year', String(year));
      // if (months?.length) months.forEach((m) => params.append('months', String(m)));
      // const response = await fetch(`${ApiUrl}/employees/${employeeId}/timesheets?${params}`);
      // return response.json();
      
      await new Promise((resolve) => setTimeout(resolve, 1200));
      // Filter mock data by year and months if provided
      let filtered = mockResponse.timesheets;
      if (year) {
        filtered = filtered.filter(t => t.year === year);
      }
      if (months && months.length > 0) {
        filtered = filtered.filter(t => months.includes(t.month));
      }
      return { ...mockResponse, timesheets: filtered };
    })(),
  };
};
