export interface PositionItem {
  position: string | null;
  workload: number | null;
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
      personalNumber: 10001,
      fullName: "Jan Novák",
      employeeType: "Akademik",
      positions: [
        { position: "01-01 – Senior Software Engineer", workload: 1.0 },
        { position: "02-03 – Konzultant", workload: 0.25 },
      ],
    },
    {
      id: "e2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d",
      personalNumber: 10002,
      fullName: "Marie Svobodová",
      employeeType: "Neakademik",
      positions: [{ position: "01-02 – Analyst", workload: 0.5 }],
    },
    {
      id: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e",
      personalNumber: 10003,
      fullName: "Petr Dvořák",
      employeeType: "Akademik",
      positions: [{ position: "03-01 – DevOps", workload: 1.0 }],
    },
  ],
};

export const getContractEmployees = (projectId: string, contractId: string) => {
  return {
    promise: (async (): Promise<GetContractEmployeesResponse> => {
      await new Promise((resolve) => setTimeout(resolve, 1200));
      return mockResponse;
    })(),
  };
};
