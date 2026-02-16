export interface ProjectContractManagerItem {
  contractId: string;
  contractName: string;
  employeeId: string;
  employeePersonalNumber: number;
  employeeFullName: string;
  employeeEmail: string;
}

export interface GetProjectContractsManagersResponse {
  managers: ProjectContractManagerItem[];
}

const mockResponse: GetProjectContractsManagersResponse = {
  managers: [
    {
      contractId: "c1a6f0f4-7c6c-4c2d-bbc2-1a1f01c9a001",
      contractName: "Vývoj informačního systému",
      employeeId: "c2a6f0f4-7c6c-4c2d-bbc2-1a1f01c9a009",
      employeePersonalNumber: 2154,
      employeeFullName: "Ing. Jan Novák",
      employeeEmail: "jan.novak@email.cz",
    },
    {
      contractId: "c1a6f0f4-7c6c-4c2d-bbc2-1a1f01c9a002",
      contractName: "Datová analýza",
      employeeId: "c2a6f0f4-7c6c-4c2d-bbc2-1a1f01c9a010",
      employeePersonalNumber: 2721,
      employeeFullName: "Mgr. David Dvořák",
      employeeEmail: "david.dvorak@email.cz",
    },
  ],
};

export const getProjectContractsManagers = (id: string) => {
  return {
    promise: (async (): Promise<GetProjectContractsManagersResponse> => {
      await new Promise((resolve) => setTimeout(resolve, 1200));
      return mockResponse;
    })(),
  };
};
