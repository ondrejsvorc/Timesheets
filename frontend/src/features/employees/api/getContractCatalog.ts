export interface ContractCatalogItem {
  id: string;
  projectId: string;
  name: string;
}

export interface GetContractCatalogResponse {
  contracts: ContractCatalogItem[];
}

const mockResponse: GetContractCatalogResponse = {
  contracts: [
    {
      id: "c1a2b3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
      projectId: "4efc77cd-0479-4b57-b2c2-11783faf4063",
      name: "Hlavní výzkumná zakázka",
    },
    {
      id: "d2b3c4a5-f6e7-4b8c-9d0e-1f2a3b4c5d6e",
      projectId: "4efc77cd-0479-4b57-b2c2-11783faf4063",
      name: "Podpora administrace projektu",
    },
    {
      id: "e3c4d5b6-a7f8-4c9d-0e1f-2a3b4c5d6e7f",
      projectId: "a8b3c5d2-1e4f-4a6b-9c8d-2f5e7a9b3c1d",
      name: "Laboratorní testování",
    },
    {
      id: "f4d5e6c7-b8a9-4d0e-1f2a-3b4c5d6e7f8a",
      projectId: "f7e9d1c3-5a8b-4c2d-9e1f-3a7b5c9d1e3f",
      name: "Vývoj softwarového modulu",
    },
    {
      id: "a5e6f7d8-c9b0-4e1f-2a3b-4c5d6e7f8a9b",
      projectId: "f7e9d1c3-5a8b-4c2d-9e1f-3a7b5c9d1e3f",
      name: "Interní analýza dat",
    },
  ],
};

export const getContractCatalog = async (projectId?: string): Promise<GetContractCatalogResponse> => {
  if (!projectId) {
    return { contracts: [] };
  }

  await new Promise((resolve) => setTimeout(resolve, 1200));

  return {
    contracts: mockResponse.contracts.filter((c) => c.projectId === projectId),
  };
};
