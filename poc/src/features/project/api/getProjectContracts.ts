import type { ProjectContractItem } from "./shared/projectContractItem";

export interface GetProjectContractsResponse {
  contracts: ProjectContractItem[];
}

const mockResponse: GetProjectContractsResponse = {
  contracts: [
    {
      id: "c1a6f0f4-7c6c-4c2d-bbc2-1a1f01c9a001",
      name: "Vývoj informačního systému",
      registrationNumber: "Z-123456",
      employeeCount: 5,
    },
    {
      id: "c1a6f0f4-7c6c-4c2d-bbc2-1a1f01c9a002",
      name: "Datová analýza",
      registrationNumber: "Z-123457",
      employeeCount: 8,
    },
  ],
};

export const getProjectContracts = (id: string) => {
  return {
    promise: (async (): Promise<GetProjectContractsResponse> => {
      await new Promise((resolve) => setTimeout(resolve, 1200));
      return mockResponse;
    })(),
  };
};
