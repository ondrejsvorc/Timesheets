import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export interface ContractCatalogItem {
  id: string;
  projectId: string;
  name: string;
}

export interface GetContractCatalogResponse {
  contracts: ContractCatalogItem[];
}

export const getContractCatalog = async (projectId?: string): Promise<GetContractCatalogResponse> => {
  if (!projectId) {
    return { contracts: [] };
  }

  const params = new URLSearchParams({ projectId });
  return withDelay("slow", () => customFetch<GetContractCatalogResponse>(`${ApiUrl}/contracts/catalog?${params.toString()}`));
};
