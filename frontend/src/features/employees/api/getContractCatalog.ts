import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

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
  return withOptionalDelay("slow", () => customFetch<GetContractCatalogResponse>(`${ApiUrl}/contracts/catalog?${params.toString()}`));
};
