export interface GetProjectContractResponse {
  id: string;
  name: string;
  registrationNumber: string;
}

const mockContracts: GetProjectContractResponse[] = [
  {
    id: "c1a6f0f4-7c6c-4c2d-bbc2-1a1f01c9a001",
    name: "Vývoj informačního systému",
    registrationNumber: "Z-123456",
  },
  {
    id: "c1a6f0f4-7c6c-4c2d-bbc2-1a1f01c9a002",
    name: "Datová analýza",
    registrationNumber: "Z-123457",
  },
];

export const getProjectContract = (projectId: string, contractId: string) => {
  return {
    promise: (async (): Promise<GetProjectContractResponse | null> => {
      await new Promise((resolve) => setTimeout(resolve, 600));
      return mockContracts.find((c) => c.id === contractId) ?? null;
    })(),
  };
};
