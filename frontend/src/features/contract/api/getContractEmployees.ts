export interface PositionItem {
  position: string | null;
  workload: number | null;
  startDate: string;
  endDate: string | null;
}

export interface EmployeeItem {
  id: string;
  personalNumber: number;
  fullName: string;
  employeeType: string;
  positions: PositionItem[];
}

export interface GetContractEmployeesResponse {
  employees: EmployeeItem[];
}

const mockResponse: GetContractEmployeesResponse = {
  employees: [
    {
      id: "e1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c",
      personalNumber: 2154,
      fullName: "Jan Novák",
      employeeType: "Neakademik",
      positions: [
        {
          position: "01-01 název pozice",
          workload: 1.0,
          startDate: "2024-03-01",
          endDate: "2025-10-31",
        },
        {
          position: "01-02 název pozice",
          workload: 0.5,
          startDate: "2024-10-01",
          endDate: "2025-11-01",
        },
      ],
    },
    {
      id: "e2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d",
      personalNumber: 2987,
      fullName: "Petra Malá",
      employeeType: "Neakademik",
      positions: [
        {
          position: "01-01 název pozice",
          workload: 1.0,
          startDate: "2023-07-15",
          endDate: "2025-12-31",
        },
      ],
    },
    {
      id: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e",
      personalNumber: 2647,
      fullName: "Karel Nový",
      employeeType: "Neakademik",
      positions: [
        {
          position: "01-01 název pozice",
          workload: 1.0,
          startDate: "2023-01-01",
          endDate: "2023-06-30",
        },
        {
          position: "01-02 název pozice",
          workload: 0.5,
          startDate: "2024-07-10",
          endDate: "2026-01-31",
        },
      ],
    },
  ],
};

export const getContractEmployees = (_projectId: string, _contractId: string) => {
  return {
    promise: (async (): Promise<GetContractEmployeesResponse> => {
      await new Promise((resolve) => setTimeout(resolve, 1200));
      return mockResponse;
    })(),
  };
};
