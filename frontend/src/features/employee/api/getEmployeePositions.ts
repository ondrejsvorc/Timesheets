export interface EmployeePositionItem {
  projectId: string;
  projectName: string;
  contractId: string;
  contractName: string;
  position: string;
  startDate: string;
  endDate: string | null;
}

export interface GetEmployeePositionsResponse {
  employeeId: string;
  positions: EmployeePositionItem[];
}
const mockResponse: GetEmployeePositionsResponse = {
  employeeId: "1f4a3b2c-8e6d-4b2a-9f3e-1c2d3e4f5a6b",
  positions: [
    {
      projectId: "8f2b9b7e-6d3e-4c6e-9b8f-1f3a9a1c2d01",
      projectName: "ERP Modernizace",
      contractId: "a1c3e5f7-1111-4a2b-9c3d-000000000001",
      contractName: "ERP Core – Analýza a návrh",
      position: "01-01 – Senior Software Engineer",
      startDate: "2023-02-01",
      endDate: "2024-12-31",
    },
    {
      projectId: "8f2b9b7e-6d3e-4c6e-9b8f-1f3a9a1c2d01",
      projectName: "ERP Modernizace",
      contractId: "a1c3e5f7-2222-4a2b-9c3d-000000000002",
      contractName: "ERP Integrace – externí systémy",
      position: "02-03 – Integration Developer",
      startDate: "2024-01-15",
      endDate: "2025-06-30",
    },
    {
      projectId: "3c7a4f92-9e44-4c5f-8e91-7d2a6b4f9001",
      projectName: "Mobilní aplikace Klient",
      contractId: "b7d9e111-3333-4c1a-8b2e-000000000003",
      contractName: "iOS aplikace – vývoj",
      position: "03-02 – iOS Developer",
      startDate: "2023-09-01",
      endDate: "2024-08-31",
    },
    {
      projectId: "3c7a4f92-9e44-4c5f-8e91-7d2a6b4f9001",
      projectName: "Mobilní aplikace Klient",
      contractId: "b7d9e111-4444-4c1a-8b2e-000000000004",
      contractName: "Backend API – mobilní klient",
      position: "01-04 – Backend Developer",
      startDate: "2023-06-01",
      endDate: "2025-03-31",
    },
    {
      projectId: "e91a55c4-2d7f-4c3b-9a2f-5a7b8c9d1001",
      projectName: "Data Warehouse",
      contractId: "c2f4a6b8-5555-4e9a-8d7c-000000000005",
      contractName: "ETL pipeline – návrh a implementace",
      position: "04-01 – Data Engineer",
      startDate: "2022-11-01",
      endDate: "2024-10-31",
    },
    {
      projectId: "e91a55c4-2d7f-4c3b-9a2f-5a7b8c9d1001",
      projectName: "Data Warehouse",
      contractId: "c2f4a6b8-6666-4e9a-8d7c-000000000006",
      contractName: "Reporting & Power BI",
      position: "05-02 – BI Analyst",
      startDate: "2024-02-01",
      endDate: "2025-12-31",
    },
    {
      projectId: "5b3c1a77-8f9d-4b6e-9c44-2d9a1f0e2001",
      projectName: "Interní HR systém",
      contractId: "d8e0f222-7777-4f3b-9a1e-000000000007",
      contractName: "HR Portál – frontend",
      position: "02-01 – Frontend Developer",
      startDate: "2023-04-01",
      endDate: "2023-12-31",
    },
    {
      projectId: "5b3c1a77-8f9d-4b6e-9c44-2d9a1f0e2001",
      projectName: "Interní HR systém",
      contractId: "d8e0f222-8888-4f3b-9a1e-000000000008",
      contractName: "HR Portál – údržba",
      position: "06-01 – Application Support",
      startDate: "2024-01-01",
      endDate: "2026-12-31",
    },
  ],
};

export const getEmployeePositions = (_employeeId: string) => {
  return {
    promise: (async (): Promise<GetEmployeePositionsResponse> => {
      await new Promise((resolve) => setTimeout(resolve, 1200));
      return mockResponse;
    })(),
  };
};
