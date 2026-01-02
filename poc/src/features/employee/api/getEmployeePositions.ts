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
  positions: EmployeePositionItem[];
}

const mockResponse: GetEmployeePositionsResponse = {
  positions: [
    {
      projectId: "11111111-1111-1111-1111-111111111111",
      projectName: "Název projektu",
      contractId: "22222222-2222-2222-2222-222222222222",
      contractName: "Název zakázky",
      position: "01-01 - název pozice",
      startDate: "2024-03-01",
      endDate: "2025-10-31",
    },
    {
      projectId: "11111111-1111-1111-1111-111111111111",
      projectName: "Název projektu",
      contractId: "33333333-3333-3333-3333-333333333333",
      contractName: "Název zakázky",
      position: "01-01 - název pozice",
      startDate: "2024-10-01",
      endDate: "2025-11-01",
    },
    {
      projectId: "44444444-4444-4444-4444-444444444444",
      projectName: "Název projektu",
      contractId: "55555555-5555-5555-5555-555555555555",
      contractName: "Název zakázky",
      position: "01-01 - název pozice",
      startDate: "2023-07-15",
      endDate: "2025-12-31",
    },
    {
      projectId: "44444444-4444-4444-4444-444444444444",
      projectName: "Název projektu",
      contractId: "66666666-6666-6666-6666-666666666666",
      contractName: "Název zakázky",
      position: "01-01 - název pozice",
      startDate: "2023-01-01",
      endDate: "2023-06-30",
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
